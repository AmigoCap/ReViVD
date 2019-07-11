using System.Globalization;
using UnityEngine.UI;

class Tools {
    public static float ParseField_f(InputField field, float ifEmpty = 0f) {
        return field.text == "" ? ifEmpty : float.Parse(field.text.Replace(',', '.'), CultureInfo.InvariantCulture);
    }

    public static int ParseField_i(InputField field, int ifEmpty = 0) {
        return field.text == "" ? ifEmpty : int.Parse(field.text);
    }
}
