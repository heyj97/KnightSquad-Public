using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>타일 길찾기. 단독 테스트와 RaidPatternMgr를 통한 실제 레이드 진행을 지원한다.</summary>
public class RaidTilePatternMgr : MonoBehaviour
{
    private const int BoardSize = 5;
    private const int AllowedMistakes = 3;
    [Header("Scene references")]
    [SerializeField] private RaidCharacter m_Player;
    [SerializeField] private Transform m_TilesRoot;
    [SerializeField] private Transform m_StartPoint;
    [SerializeField] private Transform m_EndPoint;
    [SerializeField] private Transform m_CharSpawnPoint;
    [SerializeField] private BoxCollider2D[] m_MovementBounds;
    [SerializeField] private GameObject m_MiniGameRoot;
    [SerializeField] private UnityEngine.UI.Text m_PatternTimerTxt;
    [SerializeField] private RaidPatternHeartsUI m_HeartsUI;
    [Header("Tile sprites")]
    [SerializeField] private Sprite m_NormalSprite;
    [SerializeField] private Sprite m_SafeSprite;
    [SerializeField] private Sprite m_DangerSprite;
    [Header("Pattern settings")]
    [SerializeField, Min(0.1f)] private float m_PreviewDuration = 4f;
    [SerializeField, Min(0.1f)] private float m_TimeLimit = 20f;
    [SerializeField, Min(0.01f)] private float m_DangerFlashDuration = 0.3f;
    [SerializeField, Min(0.01f)] private float m_PlayerHitFlashDuration = 0.2f;
    [SerializeField, Min(0f)] private float m_ReturnInputBlockDuration = 0.5f;
    [Header("Development only")]
    [SerializeField] private bool m_AutoStartForTesting;

    private SpriteRenderer[] m_Tiles;
    private Sprite[] m_OriginalSprites;
    private bool[] m_SafeTiles;
    private float[] m_DangerUntil;
    private Vector2 m_BottomLeft;
    private Vector2 m_CellSpacing;
    private Vector2Int m_LastCell;
    private Vector3 m_LastSafePosition;
    private Vector3 m_WalkFrom;
    private Vector3 m_ResolvedPosition;
    private float m_PreviewEndsAt;
    private bool m_Arrived;
    private bool m_FailRequested;
    private bool m_OwnsPlayer;
    private bool m_OwnsView;
    private Func<Vector2Int, bool, bool> m_CellVisitor;
    public bool IsRunning { get; private set; }
    public bool IsPreviewing { get; private set; }
    public float TimeRemaining { get; private set; }
    public int RemainingHearts { get; private set; }
    public bool? LastResult { get; private set; }
    public event Action<bool> OnPatternCompleted;
    internal bool IsRaidManaged { get; set; }

    private void Awake() => m_CellVisitor = VisitCell;

    private IEnumerator Start()
    {
        yield return null;
        while (SceneChangeMgr.IsTransitioning) yield return null;
        if (m_AutoStartForTesting && !IsRaidManaged &&
            BattleFlowMgr.CurrentMode != BattleFlowMgr.BattleMode.Raid) BeginPattern();
    }

    [ContextMenu("Development/Start tile pattern")]
    public void BeginPattern()
    {
        if (!Application.isPlaying || !isActiveAndEnabled || SceneChangeMgr.IsTransitioning) return;
        if (BattleFlowMgr.CurrentMode == BattleFlowMgr.BattleMode.Raid && !IsRaidManaged)
        {
            Debug.LogWarning("레이드 길찾기는 RaidPatternMgr를 통해 시작해주세요.", this);
            return;
        }
        CancelPattern();
        try
        {
            ValidateReferences();
            PrepareBoard();
            GenerateSafePath();
            if (!m_Player.PrepareForPreview(m_CharSpawnPoint.position))
                throw new InvalidOperationException("길찾기: 전용 캐릭터 초기화에 실패했습니다.");
            m_OwnsPlayer = true;
            if (!m_HeartsUI.Show(AllowedMistakes))
                throw new InvalidOperationException("길찾기: 하트 UI 설정을 확인해주세요.");
            m_MiniGameRoot.SetActive(true);
            m_OwnsView = true;
            m_LastCell = new Vector2Int(-1, 0);
            m_LastSafePosition = m_CharSpawnPoint.position;
            RemainingHearts = AllowedMistakes;
            TimeRemaining = m_TimeLimit;
            LastResult = null;
            m_Arrived = false;
            m_FailRequested = false;
            m_PreviewEndsAt = Time.time + m_PreviewDuration;
            IsPreviewing = true;
            IsRunning = true;
            for (int i = 0; i < m_Tiles.Length; i++)
                m_Tiles[i].sprite = m_SafeTiles[i] ? m_SafeSprite : m_DangerSprite;
            RefreshTimer();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, this);
            CancelPattern();
        }
    }

    private void ValidateReferences()
    {
        if (m_Player == null || m_TilesRoot == null || m_StartPoint == null || m_EndPoint == null ||
            m_CharSpawnPoint == null || m_MiniGameRoot == null || m_PatternTimerTxt == null ||
            m_HeartsUI == null || m_NormalSprite == null || m_SafeSprite == null || m_DangerSprite == null)
            throw new InvalidOperationException("길찾기: 씬 참조와 일반/안전/위험 스프라이트를 모두 연결해주세요.");
        if (transform.IsChildOf(m_MiniGameRoot.transform) || m_Player.transform.IsChildOf(m_MiniGameRoot.transform))
            throw new InvalidOperationException("길찾기: 미니게임 화면 안에 매니저나 PlayerRoot를 배치할 수 없습니다.");
        if (Mathf.Abs(Mathf.DeltaAngle(0f, m_TilesRoot.eulerAngles.z)) > 0.01f ||
            m_TilesRoot.lossyScale.x <= 0f || m_TilesRoot.lossyScale.y <= 0f)
            throw new InvalidOperationException("길찾기: 타일 판은 회전 없이 양수 스케일로 배치해주세요.");
        if (m_MovementBounds == null || m_MovementBounds.Length == 0)
            throw new InvalidOperationException("길찾기: MovementBounds의 BoxCollider2D들을 연결해주세요.");
        bool containsSpawn = false;
        foreach (BoxCollider2D collider in m_MovementBounds)
        {
            if (collider == null || !collider.enabled || !collider.gameObject.activeInHierarchy ||
                Mathf.Abs(Mathf.DeltaAngle(0f, collider.transform.eulerAngles.z)) > 0.01f)
                throw new InvalidOperationException("길찾기: 활성화된 회전 없는 BoxCollider2D가 필요합니다.");
            Bounds bounds = collider.bounds;
            Vector3 spawn = m_CharSpawnPoint.position;
            containsSpawn |= spawn.x >= bounds.min.x && spawn.x <= bounds.max.x &&
                spawn.y >= bounds.min.y && spawn.y <= bounds.max.y;
        }
        if (!containsSpawn) throw new InvalidOperationException("길찾기: 스폰 지점이 이동 범위 밖입니다.");
    }

    private void PrepareBoard()
    {
        var renderers = new List<SpriteRenderer>();
        var xs = new List<float>();
        var ys = new List<float>();
        foreach (Transform tile in m_TilesRoot)
        {
            SpriteRenderer renderer = tile.GetComponent<SpriteRenderer>();
            if (renderer == null) continue;
            if (!renderer.enabled || !tile.gameObject.activeInHierarchy)
                throw new InvalidOperationException("길찾기: 타일 25개를 모두 활성화해주세요.");
            renderers.Add(renderer);
            Vector3 position = m_TilesRoot.InverseTransformPoint(tile.position);
            if (!xs.Exists(x => Mathf.Abs(x - position.x) < 0.01f)) xs.Add(position.x);
            if (!ys.Exists(y => Mathf.Abs(y - position.y) < 0.01f)) ys.Add(position.y);
        }
        if (renderers.Count != BoardSize * BoardSize || xs.Count != BoardSize || ys.Count != BoardSize)
            throw new InvalidOperationException("길찾기: Tilemap 바로 아래에 5×5로 정렬한 SpriteRenderer 25개가 필요합니다.");
        xs.Sort();
        ys.Sort();
        m_BottomLeft = new Vector2(xs[0], ys[0]);
        m_CellSpacing = new Vector2(xs[1] - xs[0], ys[1] - ys[0]);
        for (int i = 1; i < BoardSize; i++)
            if (Mathf.Abs(xs[i] - xs[0] - m_CellSpacing.x * i) > 0.01f ||
                Mathf.Abs(ys[i] - ys[0] - m_CellSpacing.y * i) > 0.01f)
                throw new InvalidOperationException("길찾기: 타일의 가로/세로 간격이 일정해야 합니다.");
        m_Tiles = new SpriteRenderer[BoardSize * BoardSize];
        m_OriginalSprites = new Sprite[m_Tiles.Length];
        m_SafeTiles = new bool[m_Tiles.Length];
        m_DangerUntil = new float[m_Tiles.Length];
        foreach (SpriteRenderer renderer in renderers)
        {
            Vector2Int cell = CellAt(renderer.transform.position);
            int index = cell.y * BoardSize + cell.x;
            if (!IsBoardCell(cell) || m_Tiles[index] != null)
                throw new InvalidOperationException("길찾기: 중복되거나 범위를 벗어난 타일이 있습니다.");
            m_Tiles[index] = renderer;
            m_OriginalSprites[index] = renderer.sprite;
        }
        if (CellAt(m_StartPoint.position) != new Vector2Int(-1, 0) ||
            CellAt(m_CharSpawnPoint.position) != new Vector2Int(-1, 0) ||
            CellAt(m_EndPoint.position) != new Vector2Int(BoardSize, BoardSize - 1))
            throw new InvalidOperationException("길찾기: Start/스폰은 왼쪽 아래 칸 왼쪽, End는 오른쪽 위 칸 오른쪽에 배치해주세요.");
    }

    private void GenerateSafePath()
    {
        var directions = new List<Vector2Int>();
        for (int i = 0; i < BoardSize - 1; i++) { directions.Add(Vector2Int.right); directions.Add(Vector2Int.up); }
        for (int i = directions.Count - 1; i > 0; i--)
        {
            int other = UnityEngine.Random.Range(0, i + 1);
            (directions[i], directions[other]) = (directions[other], directions[i]);
        }
        Vector2Int cell = Vector2Int.zero;
        m_SafeTiles[0] = true;
        foreach (Vector2Int direction in directions)
        {
            cell += direction;
            m_SafeTiles[cell.y * BoardSize + cell.x] = true;
        }
    }

    private Vector2 GridPosition(Vector3 worldPosition)
    {
        Vector3 local = m_TilesRoot.InverseTransformPoint(worldPosition);
        return new Vector2((local.x - m_BottomLeft.x) / m_CellSpacing.x + 0.5f,
            (local.y - m_BottomLeft.y) / m_CellSpacing.y + 0.5f);
    }

    private Vector2Int CellAt(Vector3 worldPosition)
    {
        Vector2 position = GridPosition(worldPosition);
        return new Vector2Int(Mathf.FloorToInt(position.x), Mathf.FloorToInt(position.y));
    }

    private static bool IsBoardCell(Vector2Int cell) =>
        cell.x >= 0 && cell.x < BoardSize && cell.y >= 0 && cell.y < BoardSize;

    private Vector3 ResolveMovement(Vector3 from, Vector3 candidate)
    {
        if (!IsRunning || IsPreviewing || m_Arrived || m_FailRequested) return from;
        m_WalkFrom = from;
        m_ResolvedPosition = candidate;
        RaidTilePathUtility.VisitCells(GridPosition(from), GridPosition(candidate), m_CellVisitor);
        return m_ResolvedPosition;
    }

    private bool VisitCell(Vector2Int cell, bool touchOnly)
    {
        if (cell == m_LastCell) return true;
        if (cell == new Vector2Int(-1, 0))
        {
            if (!touchOnly) { m_LastCell = cell; m_LastSafePosition = m_CharSpawnPoint.position; }
            return true;
        }
        if (cell == new Vector2Int(BoardSize, BoardSize - 1))
        {
            if (!touchOnly && m_LastCell == new Vector2Int(BoardSize - 1, BoardSize - 1))
            {
                m_Arrived = true;
                return true;
            }
            m_ResolvedPosition = m_WalkFrom;
            return false;
        }
        if (!IsBoardCell(cell))
        {
            m_ResolvedPosition = m_WalkFrom;
            return false;
        }
        int index = cell.y * BoardSize + cell.x;
        if (!m_SafeTiles[index])
        {
            RemainingHearts--;
            m_HeartsUI.SetRemaining(RemainingHearts);
            m_Tiles[index].sprite = m_DangerSprite;
            m_DangerUntil[index] = Time.time + m_DangerFlashDuration;
            m_Player.ShowHitFlash(m_PlayerHitFlashDuration);
            m_Player.BlockControlFor(m_ReturnInputBlockDuration);
            m_FailRequested = RemainingHearts <= 0;
            m_ResolvedPosition = m_LastSafePosition;
            return false;
        }
        if (!touchOnly)
        {
            m_Tiles[index].sprite = m_SafeSprite;
            m_LastCell = cell;
            Vector3 center = m_Tiles[index].transform.position;
            center.z = m_CharSpawnPoint.position.z;
            m_LastSafePosition = center;
        }
        return true;
    }

    private void LateUpdate()
    {
        if (!IsRunning) return;
        if (IsPreviewing)
        {
            RefreshTimer();
            if (Time.time < m_PreviewEndsAt) return;
            IsPreviewing = false;
            ShowNormalTiles();
            float step = Mathf.Min(m_CellSpacing.x * m_TilesRoot.lossyScale.x,
                m_CellSpacing.y * m_TilesRoot.lossyScale.y) * 0.2f;
            if (!m_Player.BeginFreeMovement(m_MovementBounds, m_CharSpawnPoint.position, ResolveMovement, step))
            {
                Debug.LogError("길찾기: 이동 범위 또는 캐릭터 설정을 확인해주세요.", this);
                Complete(false);
            }
            RefreshTimer();
            return;
        }
        for (int i = 0; i < m_DangerUntil.Length; i++)
            if (m_DangerUntil[i] > 0f && Time.time >= m_DangerUntil[i])
            {
                m_Tiles[i].sprite = m_NormalSprite;
                m_DangerUntil[i] = 0f;
            }
        TimeRemaining = Mathf.Max(0f, TimeRemaining - Time.deltaTime);
        RefreshTimer();
        if (m_Arrived) Complete(true);
        else if (m_FailRequested || TimeRemaining <= 0f) Complete(false);
    }

    private void ShowNormalTiles()
    {
        if (m_Tiles == null) return;
        for (int i = 0; i < m_Tiles.Length; i++)
            if (m_Tiles[i] != null) m_Tiles[i].sprite = m_NormalSprite;
    }

    private void RefreshTimer()
    {
        if (m_PatternTimerTxt != null)
            m_PatternTimerTxt.text = IsPreviewing
                ? $"경로를 기억하세요\n{Mathf.Max(0f, m_PreviewEndsAt - Time.time):0.0}"
                : $"남은 시간\n{TimeRemaining:0.00}";
    }

    private void Complete(bool succeeded)
    {
        if (!IsRunning) return;
        IsRunning = false;
        IsPreviewing = false;
        LastResult = succeeded;
        m_Player.EndControl();
        m_HeartsUI.Hide();
        // 밟은 안전 칸은 결과 화면에도 유지하고 위험 표시만 정리한다.
        for (int i = 0; i < m_DangerUntil.Length; i++)
            if (m_DangerUntil[i] > 0f)
            {
                m_Tiles[i].sprite = m_NormalSprite;
                m_DangerUntil[i] = 0f;
            }
        RefreshTimer();
        Debug.Log(succeeded ? "타일 길찾기 패턴 성공" : "타일 길찾기 패턴 실패", this);
        OnPatternCompleted?.Invoke(succeeded);
    }

    public void CancelPattern()
    {
        IsRunning = false;
        IsPreviewing = false;
        if (m_OwnsPlayer && m_Player != null) { m_Player.EndControl(); m_Player.ClearHitFlash(); }
        if (m_OwnsView && m_MiniGameRoot != null) m_MiniGameRoot.SetActive(false);
        if (m_HeartsUI != null) m_HeartsUI.Hide();
        if (m_Tiles != null)
            for (int i = 0; i < m_Tiles.Length; i++)
                if (m_Tiles[i] != null) m_Tiles[i].sprite = m_OriginalSprites[i];
        m_Tiles = null;
        m_OriginalSprites = null;
        m_SafeTiles = null;
        m_DangerUntil = null;
        m_OwnsPlayer = false;
        m_OwnsView = false;
        m_Arrived = false;
        m_FailRequested = false;
        LastResult = null;
        TimeRemaining = 0f;
        RemainingHearts = 0;
    }

    private void OnDisable() => CancelPattern();
}
