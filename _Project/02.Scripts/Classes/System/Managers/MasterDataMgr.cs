using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 캐릭터, 몬스터, 스킬, 아이템의 정적 데이터를 ID로 조회하는 읽기 전용 레지스트리다.
/// 중복Id, 빈ID, 누락 참조 검증을 한다.
/// SO는 characterId, monsterId, skillId, ItemId를 기본 키로 사용하고 있기 때문에
/// 이 매니저가 런타임과 데이터를 연결해주는 접점이 된다.
/// </summary>
/// <remarks>
///<list type="bullet">
///<item><description><c>public bool TryGetCharacter(string id, out CharacterData data)</c><br></br>
/// string id를 기반으로 캐릭터 정보를 반환하고 성공여부를 true/false로 반환한다.
/// </description></item>
///<item><description><c>public bool TryGetMonster(string id, out MonsterData data)</c><br></br>
/// string id를 기반으로 몬스터 정보를 반환하고 성공여부를 true/false로 반환한다.
/// </description></item>
///<item><description><c>public bool TryGetEquipment(string id, out EquipmentData data)</c><br></br>
/// string id를 기반으로 장비아이템 정보를 반환하고 성공여부를 true/false로 반환한다.
/// </description></item>
///<item><description><c>public bool TryGetSkill(string id, out SkillData data)</c><br></br>
/// string id를 기반으로 스킬 정보를 반환하고 성공여부를 true/false로 반환한다.
/// </description></item>
/// </list>
/// </remarks>
[DefaultExecutionOrder(-1000)]
public class MasterDataMgr : G_Singleton<MasterDataMgr>
{
    [SerializeField] private List<CharacterData> characters;
    private readonly Dictionary<string, CharacterData> _characterMap = new();
    /// <summary>등록된 전체 캐릭터 목록의 사본. SO 등록 후 일반 뽑기 풀에 자동 반영된다.</summary>
    public IReadOnlyList<CharacterData> GetCharacters() => new List<CharacterData>(_characterMap.Values).AsReadOnly();
    
    [SerializeField] private List<MonsterData> monsters;
    private readonly Dictionary<string, MonsterData> _monsterMap = new();
    
    [SerializeField] private List<EquipmentData> equipItems;
    private readonly Dictionary<string, EquipmentData> _equipItemMap = new();
    
    [SerializeField] private List<SkillData> skills;
    private readonly Dictionary<string, SkillData> _skillMap = new();

    protected override void Awake()
    {
        base.Awake();

        // 캐릭터 데이터를 불러와서 메모리에 보유하고 있는다.
        foreach (CharacterData data in characters)
        {
            // 데이터가 없거나, characterId가 없으면 넘어간다.
            if (data == null || string.IsNullOrWhiteSpace(data.Id))
                continue;
            
            // Dictionary의 Key는 중복이 불가능하므로 중복체크를 Id값 기준으로 한다.
            if (!_characterMap.TryAdd(data.Id, data) || string.IsNullOrEmpty(data.Id))
            {
                PDebug.LogError($"중복 또는 빈 Character Id: {data.Id}");
                return;
            }
        }
        
        // 몬스터 데이터를 불러와서 메모리에 보유하고 있는다.
        foreach (MonsterData data in monsters)
        {
            if (data == null || string.IsNullOrWhiteSpace(data.Id))
                continue;

            if (!_monsterMap.TryAdd(data.Id, data) || string.IsNullOrEmpty(data.Id))
            {
                PDebug.LogError($"중복 또는 빈 Monster Id: {data.Id}");
                return;
            }
        }
        
        // 장비 데이터를 불러와서 메모리에 보유하고 있는다.
        foreach (EquipmentData data in equipItems)
        {
            if (data == null || string.IsNullOrWhiteSpace(data.Id))
                continue;

            if (!_equipItemMap.TryAdd(data.Id, data) || string.IsNullOrEmpty(data.Id))
            {
                PDebug.LogError($"중복 또는 빈 Equipitem Id: {data.Id}");
                return;
            }
        }
        
        // 재료아이템 데이터를 불러와서 메모리에 보유하고 있는다. -> To-do
        // foreach (MaterialData data in MaterialItems)
        // {
        //     if (data == null || string.IsNullOrWhiteSpace(data.Id))
        //         continue;
        //     
        //     if (!materialItemMap.TryAdd(data.Id, data))
        //         PDebug.LogError($"중복 Materialitem Id Id: {data.Id}");
        // }

        // 스킬 데이터를 불러와서 메모리에 보유하고 있는다.
        foreach (SkillData data in skills)
        {
            if (data == null || string.IsNullOrWhiteSpace(data.Id))
                continue;

            if (!_skillMap.TryAdd(data.Id, data) || string.IsNullOrEmpty(data.Id))
            {
                PDebug.LogError($"중복 또는 빈 Skill Id: {data.Id}");
                return;
            }
        }
    }
    
    // id를 기반으로 characterMap에서 캐릭터데이터를 찾아서 반환한다.
    /// <summary>
    /// 캐릭터 ID로 캐릭터 마스터 데이터를 조회합니다.
    /// </summary>
    /// <param name="id">조회할 마스터 데이터의 ID입니다.</param>
    /// <param name="data">초기화하거나 계산에 사용할 마스터 데이터입니다.</param>
    /// <returns>작업이 성공하면 <see langword="true"/>, 사전 조건이 충족되지 않거나 처리에 실패하면 <see langword="false"/>입니다.</returns>
    public bool TryGetCharacter(string id, out CharacterData data)
        => _characterMap.TryGetValue(id, out data);
    
    // id를 기반으로 monsterMap에서 몬스터데이터를 찾아서 반환한다.
    /// <summary>
    /// 몬스터 ID로 몬스터 마스터 데이터를 조회합니다.
    /// </summary>
    /// <param name="id">조회할 마스터 데이터의 ID입니다.</param>
    /// <param name="data">초기화하거나 계산에 사용할 마스터 데이터입니다.</param>
    /// <returns>작업이 성공하면 <see langword="true"/>, 사전 조건이 충족되지 않거나 처리에 실패하면 <see langword="false"/>입니다.</returns>
    public bool TryGetMonster(string id, out MonsterData data)
        => _monsterMap.TryGetValue(id, out data);
    
    // id를 기반으로 equipItemMap에서 장비아이템데이터를 찾아서 반환한다.
    /// <summary>
    /// 장비 ID로 장비 마스터 데이터를 조회합니다.
    /// </summary>
    /// <param name="id">조회할 마스터 데이터의 ID입니다.</param>
    /// <param name="data">초기화하거나 계산에 사용할 마스터 데이터입니다.</param>
    /// <returns>작업이 성공하면 <see langword="true"/>, 사전 조건이 충족되지 않거나 처리에 실패하면 <see langword="false"/>입니다.</returns>
    public bool TryGetEquipment(string id, out EquipmentData data)
        => _equipItemMap.TryGetValue(id, out data);
    
    // id를 기반으로 materialItemMap에서 재료아이템데이터를 찾아서 반환한다.   to-do
    // public bool TryGetMaterial(string id, out MaterialData data)
    //     => materialItemMap.TryGetValue(id, out data);
    
    // id를 기반으로 skillMap에서 스킬데이터를 찾아서 반환한다.
    /// <summary>
    /// 스킬 ID로 스킬 마스터 데이터를 조회합니다.
    /// </summary>
    /// <param name="id">조회할 마스터 데이터의 ID입니다.</param>
    /// <param name="data">초기화하거나 계산에 사용할 마스터 데이터입니다.</param>
    /// <returns>작업이 성공하면 <see langword="true"/>, 사전 조건이 충족되지 않거나 처리에 실패하면 <see langword="false"/>입니다.</returns>
    public bool TryGetSkill(string id, out SkillData data)
        => _skillMap.TryGetValue(id, out data);

    /// <summary>
    /// 지정 챕터에 출현 가능한 몬스터 마스터 목록을 조회합니다.
    /// </summary>
    /// <param name="chapter">출현 몬스터 목록을 조회할 챕터 번호입니다.</param>
    /// <param name="datas">조회된 마스터 데이터 목록입니다.</param>
    /// <returns>작업이 성공하면 <see langword="true"/>, 사전 조건이 충족되지 않거나 처리에 실패하면 <see langword="false"/>입니다.</returns>
    public bool TryGetMonsterDataByChapter(int chapter, out List<MonsterData> datas)
    {
        datas = new();
    
        if (chapter < 1)
            return false;
    
        const int chaptersPerPool = 3;
    
        // 3챕터에 한번씩 풀이 바뀐다.
        // 1~3 → 1, 4~6 → 4, 7~9 → 7
        int poolStartChapter =
            ((chapter - 1) / chaptersPerPool) * chaptersPerPool + 1;
    
        foreach (MonsterData monData in monsters)
        {
            if (monData != null && monData.SpawnChapter == poolStartChapter)
                datas.Add(monData);
        }
    
        return datas.Count > 0;
    }
}
