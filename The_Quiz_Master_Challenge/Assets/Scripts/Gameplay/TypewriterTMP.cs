using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;

public enum TypewriterStepMode
{
    Characters,
    Words
}

public static class TypewriterTMP
{
    public static IEnumerator Animate(TMP_Text tmp, string fullText, TypewriterStepMode mode, float stepDelaySeconds)
    {
        if (tmp == null) yield break;
        tmp.text = string.Empty;
        if (string.IsNullOrEmpty(fullText)) yield break;

        if (mode == TypewriterStepMode.Words)
        {
            var words = fullText.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
            var sb = new StringBuilder();
            for (var w = 0; w < words.Length; w++)
            {
                if (w > 0) sb.Append(' ');
                sb.Append(words[w]);
                tmp.text = sb.ToString();
                if (stepDelaySeconds > 0f)
                    yield return new WaitForSecondsRealtime(stepDelaySeconds);
            }
        }
        else
        {
            for (var c = 1; c <= fullText.Length; c++)
            {
                tmp.text = fullText.Substring(0, c);
                if (stepDelaySeconds > 0f)
                    yield return new WaitForSecondsRealtime(stepDelaySeconds);
            }
        }
    }
}
