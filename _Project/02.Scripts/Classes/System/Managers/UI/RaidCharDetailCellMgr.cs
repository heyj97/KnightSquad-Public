using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

public class RaidCharDetailCellMgr : MonoBehaviour
{
    [SerializeField] private Image m_CharProfileImg;
    [SerializeField] private Image m_RoleImg;
    [SerializeField] private Text m_CharNameTxt;
    [SerializeField] private Text m_DmgDealtTxt;
    [SerializeField] private Image m_ChartFillImg;
    [SerializeField] private Text m_ChartPercentageTxt;

    public void Show(RaidCombatResult.CharacterRecord record, double total, Sprite[] roles)
    {
        var data = record.Data;
        if (m_CharProfileImg != null) m_CharProfileImg.sprite = data != null ? data.Icon : null;
        int role = data != null ? (int)data.Role : -1;
        if (m_RoleImg != null) m_RoleImg.sprite = roles != null && role >= 0 && role < roles.Length ? roles[role] : null;
        string name = data != null ? data.DisplayName : "";
        if (m_CharNameTxt != null) m_CharNameTxt.text = name.Substring(name.LastIndexOf(' ') + 1);
        if (m_DmgDealtTxt != null)
            m_DmgDealtTxt.text = Math.Round(record.Damage, 0, MidpointRounding.AwayFromZero).ToString("N0", CultureInfo.InvariantCulture);
        float ratio = total > 0 ? Mathf.Clamp01((float)(record.Damage / total)) : 0f;
        if (m_ChartFillImg != null) m_ChartFillImg.fillAmount = ratio;
        if (m_ChartPercentageTxt != null) m_ChartPercentageTxt.text = (ratio * 100f).ToString("F1", CultureInfo.InvariantCulture) + "%";
    }
}
