using System.Text;
using TMPro;
using UnityEngine;

public class PowerupTooltip : MonoBehaviour
{
    Transform target;
    Vector3 offset;
    TextMeshPro text;
    string baseText;
    string promptSuffix;

    public static PowerupTooltip Create(Transform target, UpgradePathData data, bool rare, Powerup.TooltipSettings s)
    {
        if (target == null || data == null || s == null || !s.show) return null;

        GameObject go = new GameObject("PowerupTooltip (" + data.name + ")");
        go.transform.position = target.position + s.offset;

        TextMeshPro text = go.AddComponent<TextMeshPro>();
        text.text = BuildText(data, rare, s);
        text.fontSize = s.fontSize;
        text.color = rare ? data.rareTint : s.titleColor;
        text.alignment = TextAlignmentOptions.Bottom;
        text.rectTransform.pivot = new Vector2(0.5f, 0f);
        text.rectTransform.sizeDelta = new Vector2(s.width, s.height);

        if (go.TryGetComponent(out MeshRenderer mr))
            mr.sortingOrder = s.sortingOrder;

        PowerupTooltip tip = go.AddComponent<PowerupTooltip>();
        tip.target = target;
        tip.offset = s.offset;
        tip.text = text;
        tip.baseText = text.text;
        tip.promptSuffix = "\n<size=70%>[" + s.promptKey + "]</size>";

        return tip;
    }

    public void SetPrompt(bool visible)
    {
        if (text == null) return;

        text.text = visible ? baseText + promptSuffix : baseText;
    }

    void LateUpdate()
    {
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        transform.position = target.position + offset;
    }

    static string BuildText(UpgradePathData data, bool rare, Powerup.TooltipSettings s)
    {
        PlayerStats stats = PlayerStats.Instance;
        StringBuilder sb = new StringBuilder();

        if (rare)
        {
            string rareName = string.IsNullOrEmpty(data.rareDisplayName) ? "Rare Upgrade" : data.rareDisplayName;

            sb.Append("<b>").Append(rareName.ToUpperInvariant()).Append("</b>");

            if (s.showRareDescription && !string.IsNullOrEmpty(data.rareDescription))
                sb.Append("\n<size=75%>").Append(data.rareDescription).Append("</size>");

            return sb.ToString();
        }

        int nextLevel = (stats != null ? stats.GetLevel(data.path) : 0) + 1;
        string pathName = string.IsNullOrEmpty(data.displayName) ? data.path.ToString() : data.displayName;

        sb.Append("<b>").Append(pathName.ToUpperInvariant()).Append("</b>");
        sb.Append("\n<size=75%>Level ").Append(nextLevel).Append("</size>");

        return sb.ToString();
    }
}
