using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 설정 패널의 닫기 동작을 연결하는 UI 컴포넌트입니다.
/// </summary>
public class SettingPanelMgr : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject m_SettingPanel;
    
    [SerializeField] private Slider m_GeneralVolSlider;
    [SerializeField] private Text m_GeneralVolValueText;
    
    [SerializeField] private Slider m_BgmVolSlider;
    [SerializeField] private Text m_BgmVolValueText;
    
    [SerializeField] private Slider m_SfxVolSlider;
    [SerializeField] private Text m_SfxVolValueText;
    
    [SerializeField] private Button m_SubmitBtn;
    [SerializeField] private Button m_CloseBtn;
    [SerializeField] private Button m_QuitAfterSaveBtn;
    
    
    

    void Start()
    {
        if (m_QuitAfterSaveBtn != null)
            m_QuitAfterSaveBtn.onClick.AddListener(OnQuitAfterSaveClick);

        if (m_CloseBtn != null)
            m_CloseBtn.onClick.AddListener(() => m_SettingPanel.SetActive(false));
        
        if (m_SubmitBtn != null)
            m_SubmitBtn.onClick.AddListener(() => m_SettingPanel.SetActive(false));
        
        if (m_GeneralVolSlider != null)
            m_GeneralVolSlider.onValueChanged.AddListener(OnGeneralVolChange);
        
        if (m_BgmVolSlider != null)
            m_BgmVolSlider.onValueChanged.AddListener(OnBgmVolChange);
        
        if (m_SfxVolSlider != null)
            m_SfxVolSlider.onValueChanged.AddListener(OnSfxVolChange);
    }

    private void OnQuitAfterSaveClick()
    {
        PlayerDataMgr.Inst.SaveAndQuit();
    }

    private void OnDestroy()
    {
        if (m_QuitAfterSaveBtn != null)
            m_QuitAfterSaveBtn.onClick.RemoveListener(OnQuitAfterSaveClick);
    }
    
    void OnGeneralVolChange(float value)
    {
        // 전체 볼륨 변경 로직
        m_GeneralVolValueText.text = VolumeParser(value);
    }
    
    void OnBgmVolChange(float value)
    {
        // 배경음악 볼륨 변경 로직
        m_BgmVolValueText.text = VolumeParser(value);
    }
    
    void OnSfxVolChange(float value)
    {
        // 효과음 볼륨 변경 로직
        m_SfxVolValueText.text = VolumeParser(value);
    }

    string VolumeParser(float value)
    {
        return (value * 100).ToString("F1");    // 0.0~100.0 까지 0.1단위로 표시
    }
}
