using System.Globalization;
using UnityEngine.UI;

class Tools {
    public static float ParseField_f(InputField field, float ifEmpty = 0f) {
        if (field.text == "")
            return ifEmpty;
        if (float.TryParse(field.text.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out float result))
            return result;
        return field.text[0] == '-' ? float.MinValue : float.MaxValue;
    }

    public static int ParseField_i(InputField field, int ifEmpty = 0) {
        if (field.text == "")
            return ifEmpty;
        if (int.TryParse(field.text, out int result))
            return result;
        return field.text[0] == '-' ? int.MinValue : int.MaxValue;
    }
}
