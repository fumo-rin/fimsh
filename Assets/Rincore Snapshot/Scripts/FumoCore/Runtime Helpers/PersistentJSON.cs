using System.IO;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Security.Cryptography;
using System.Text;
#if UNITY_EDITOR
using UnityEditor;
public class PersistentJSONViewer : EditorWindow
{
    private string inputJson = "";
    private string decryptedJson = "";
    private Vector2 scrollPosInput;
    private Vector2 scrollPosOutput;

    [MenuItem("Fumorin/Json Decoder & Viewer")]
    public static void OpenWindow()
    {
        PersistentJSONViewer window = GetWindow<PersistentJSONViewer>("PersistentJSON Viewer");
        window.minSize = new Vector2(600, 400);
        window.Show();
    }

    private void OnGUI()
    {
        GUILayout.Label("PersistentJSON Viewer & Decryptor", EditorStyles.boldLabel);

        EditorGUILayout.Space();

        GUILayout.Label("Input JSON (encrypted or plain):");
        scrollPosInput = EditorGUILayout.BeginScrollView(scrollPosInput, GUILayout.Height(120));
        inputJson = EditorGUILayout.TextArea(inputJson, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();

        if (GUILayout.Button("Decrypt & Format JSON"))
        {
            DecryptAndFormatJson();
        }

        EditorGUILayout.Space();

        GUILayout.Label("Decrypted & Formatted JSON:");
        scrollPosOutput = EditorGUILayout.BeginScrollView(scrollPosOutput, GUILayout.ExpandHeight(true));
        EditorGUILayout.TextArea(decryptedJson, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();

        if (!string.IsNullOrEmpty(decryptedJson))
        {
            if (GUILayout.Button("Copy Decrypted JSON to Clipboard"))
            {
                EditorGUIUtility.systemCopyBuffer = decryptedJson;
                Debug.Log("[PersistentJSONViewer] Decrypted JSON copied to clipboard.");
            }
        }
    }
    private void DecryptAndFormatJson()
    {
        if (string.IsNullOrEmpty(inputJson))
        {
            Debug.LogWarning("[PersistentJSONViewer] Input JSON is empty.");
            decryptedJson = "";
            return;
        }

        try
        {
            // Decrypt first (if encrypted)
            string decrypted = inputJson.DecryptString();

            // Format JSON reliably
            decryptedJson = PrettyPrintJson(decrypted);

            Debug.Log("[PersistentJSONViewer] Successfully decrypted & formatted JSON.");
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[PersistentJSONViewer] Failed to decrypt JSON: " + ex.Message);
            decryptedJson = inputJson; // fallback: just show raw input
        }
    }

    // Robust pretty printer for any JSON string
    private string PrettyPrintJson(string json)
    {
        if (string.IsNullOrEmpty(json)) return "";

        int indent = 0;
        bool inQuotes = false;
        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        for (int i = 0; i < json.Length; i++)
        {
            char c = json[i];

            switch (c)
            {
                case '{':
                case '[':
                    sb.Append(c);
                    if (!inQuotes)
                    {
                        sb.AppendLine();
                        indent++;
                        sb.Append(new string(' ', indent * 2));
                    }
                    break;
                case '}':
                case ']':
                    if (!inQuotes)
                    {
                        sb.AppendLine();
                        indent--;
                        sb.Append(new string(' ', indent * 2));
                    }
                    sb.Append(c);
                    break;
                case ',':
                    sb.Append(c);
                    if (!inQuotes)
                    {
                        sb.AppendLine();
                        sb.Append(new string(' ', indent * 2));
                    }
                    break;
                case '"':
                    sb.Append(c);
                    // Ignore escaped quotes
                    bool escaped = i > 0 && json[i - 1] == '\\';
                    if (!escaped)
                        inQuotes = !inQuotes;
                    break;
                default:
                    sb.Append(c);
                    break;
            }
        }

        return sb.ToString();
    }
}
#endif
#region Parser Addon
public static partial class PersistentJSON
{
    // ---------- Public API ----------

    public static string ToJson<T>(this T obj, bool pretty = false, bool encrypted = true)
    {
        if (obj == null) return null;

        Type t = typeof(T);


        if (IsList(t))
        {
            string json = JsonUtility.ToJson(WrapList(obj), pretty);
            if (encrypted) json = json.EncryptString();
            return json;
        }

        if (IsDictionary(t))
        {
            string json = JsonUtility.ToJson(WrapDictionary(obj), pretty);
            if (encrypted) json = json.EncryptString();
            return json;
        }

        string json2 = JsonUtility.ToJson(obj, pretty);
        if (encrypted) json2 = json2.EncryptString();
        return json2;
    }
    public static bool TryFromJson<T>(this string json, out T result, bool encrypted = true)
    {
        result = default;

        if (string.IsNullOrEmpty(json))
            return false;

        Type t = typeof(T);

        bool TryDeserialize(string s, out T r)
        {
            r = default;

            try
            {
                if (IsList(t))
                {
                    object wrapper = JsonUtility.FromJson(s, GetListWrapperType(t));
                    r = (T)UnwrapList(wrapper);
                    return true;
                }

                if (IsDictionary(t))
                {
                    object wrapper = JsonUtility.FromJson(s, GetDictionaryWrapperType(t));
                    r = (T)UnwrapDictionary(wrapper);
                    return true;
                }

                r = JsonUtility.FromJson<T>(s);
                return r != null;
            }
            catch
            {
                r = default;
                return false;
            }
        }

        if (TryDeserialize(json, out result))
            return true;

        if (encrypted)
        {
            try
            {
                string decrypted = json.DecryptString();
                if (TryDeserialize(decrypted, out result))
                    return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[JsonParserAddon] Decryption failed: {ex.Message}");
            }
        }
        try
        {
            string trimmed = json.Trim();
            if (TryDeserialize(trimmed, out result))
                return true;
        }
        catch { }
        Debug.LogWarning($"[JsonParserAddon] Failed to parse JSON into {typeof(T).Name}");
        result = default;
        return false;
    }

    // ---------- List Support ----------

    private static bool IsList(Type t) =>
        t.IsGenericType && t.GetGenericTypeDefinition() == typeof(List<>);

    private static object WrapList(object list)
    {
        Type elementType = list.GetType().GetGenericArguments()[0];
        Type wrapperType = typeof(ListWrapper<>).MakeGenericType(elementType);

        object wrapper = Activator.CreateInstance(wrapperType);
        wrapperType.GetField("Items").SetValue(wrapper, list);

        return wrapper;
    }

    private static Type GetListWrapperType(Type listType)
    {
        Type elementType = listType.GetGenericArguments()[0];
        return typeof(ListWrapper<>).MakeGenericType(elementType);
    }

    private static object UnwrapList(object wrapper)
    {
        return wrapper.GetType().GetField("Items").GetValue(wrapper);
    }

    // ---------- Dictionary Support ----------

    [Serializable]
    private class DictionaryWrapper<TKey, TValue>
    {
        public List<KeyValue<TKey, TValue>> Items = new();
    }

    [Serializable]
    private class KeyValue<TKey, TValue>
    {
        public TKey Key;
        public TValue Value;
    }

    private static bool IsDictionary(Type t) =>
        t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Dictionary<,>);

    private static object WrapDictionary(object dict)
    {
        Type[] args = dict.GetType().GetGenericArguments();
        Type keyType = args[0];
        Type valueType = args[1];

        Type wrapperType = typeof(DictionaryWrapper<,>).MakeGenericType(keyType, valueType);
        Type kvType = typeof(KeyValue<,>).MakeGenericType(keyType, valueType);

        object wrapper = Activator.CreateInstance(wrapperType);
        var itemsField = wrapperType.GetField("Items");
        var itemsList = itemsField.GetValue(wrapper);

        foreach (var entry in (System.Collections.IDictionary)dict)
        {
            object kv = Activator.CreateInstance(kvType);
            kvType.GetField("Key").SetValue(kv, entry.GetType().GetProperty("Key").GetValue(entry));
            kvType.GetField("Value").SetValue(kv, entry.GetType().GetProperty("Value").GetValue(entry));
            itemsList.GetType().GetMethod("Add").Invoke(itemsList, new[] { kv });
        }

        return wrapper;
    }

    private static Type GetDictionaryWrapperType(Type dictType)
    {
        Type[] args = dictType.GetGenericArguments();
        return typeof(DictionaryWrapper<,>).MakeGenericType(args[0], args[1]);
    }

    private static object UnwrapDictionary(object wrapper)
    {
        Type wrapperType = wrapper.GetType();
        var items = (System.Collections.IEnumerable)wrapperType.GetField("Items").GetValue(wrapper);

        Type[] args = wrapperType.GetGenericArguments();
        Type dictType = typeof(Dictionary<,>).MakeGenericType(args[0], args[1]);
        var dict = (System.Collections.IDictionary)Activator.CreateInstance(dictType);

        foreach (var kv in items)
        {
            var kvType = kv.GetType();
            object key = kvType.GetField("Key").GetValue(kv);
            object value = kvType.GetField("Value").GetValue(kv);
            dict.Add(key, value);
        }

        return dict;
    }
}
#endregion
#region JSON Playerprefs Alternate during WEBGL
public static partial class PersistentJSON
{
    private static bool IsWebGLBuild =>
        Application.platform == RuntimePlatform.WebGLPlayer;
    private static bool TrySaveWebGL<T>(T saveItem, string key, string json)
    {
        PlayerPrefs.SetString(key, json);
        PlayerPrefs.Save();
        if (DebugMode)
            Debug.Log($"[WebGL] Saved {typeof(T).Name} to PlayerPrefs key '{key}'");
        return true;
    }
    private static bool TryLoadWebGL(out string json, string key)
    {
        json = null;
        if (!PlayerPrefs.HasKey(key))
        {
            if (DebugMode)
                Debug.LogWarning($"[WebGL] No PlayerPrefs key found for '{key}'");
            return false;
        }
        json = PlayerPrefs.GetString(key);
        if (DebugMode)
            Debug.Log($"[WebGL] Loaded JSON string for '{key}'");
        return true;
    }
    public static bool TryDeleteWebGL(string key)
    {
        if (!PlayerPrefs.HasKey(key))
            return false;
        PlayerPrefs.DeleteKey(key);
        PlayerPrefs.Save();
        if (DebugMode)
            Debug.Log($"[WebGL] Deleted PlayerPrefs key '{key}'");
        return true;
    }
}
#endregion
#region Safe (lmao) Score Storage
public static partial class PersistentJSON
{
    private const string EncryptionKey = "Fumo Fumo Fumo Fumo";
    public static long ToLong(this double value) =>
        BitConverter.DoubleToInt64Bits(value);
    public static double ToDouble(this long bits) =>
        BitConverter.Int64BitsToDouble(bits);
    public static string EncryptString(this string plainText, string salt = "Mofumofumo")
    {
        using (Aes aes = Aes.Create())
        {
            var key = new Rfc2898DeriveBytes(EncryptionKey, Encoding.UTF8.GetBytes(salt));
            aes.Key = key.GetBytes(32);
            aes.IV = key.GetBytes(16);

            using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
            {
                byte[] bytes = Encoding.UTF8.GetBytes(plainText);
                byte[] encrypted = encryptor.TransformFinalBlock(bytes, 0, bytes.Length);
                return Convert.ToBase64String(encrypted);
            }
        }
    }
    public static string DecryptString(this string cipherText, string salt = "Mofumofumo")
    {
        using (Aes aes = Aes.Create())
        {
            var key = new Rfc2898DeriveBytes(EncryptionKey, Encoding.UTF8.GetBytes(salt));
            aes.Key = key.GetBytes(32);
            aes.IV = key.GetBytes(16);

            using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
            {
                byte[] bytes = Convert.FromBase64String(cipherText);
                byte[] decrypted = decryptor.TransformFinalBlock(bytes, 0, bytes.Length);
                return Encoding.UTF8.GetString(decrypted);
            }
        }
    }
    private static string ComputeHash(string value)
    {
        using (SHA256 sha = SHA256.Create())
        {
            byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(value + EncryptionKey));
            return Convert.ToBase64String(bytes);
        }
    }
    [QFSW.QC.Command("-test-score-store")]
    public static bool SaveScore(double score, string key)
    {
        long encoded = score.ToLong();
        string data = encoded.ToString();
        string hash = ComputeHash(data);
        string combined = $"{data}:{hash}";
        string encrypted = combined.EncryptString();

        return PersistentJSON.TrySave(encrypted, key);
    }
    [QFSW.QC.Command("-test-score-fetch")]
    private static double TestFetchScore(string key)
    {
        double score = 0d;
        if (!LoadScore(key, out score))
        {

        }
        return score;
    }
    public static bool LoadScore(string key, out double score)
    {
        score = 0d;
        if (!PersistentJSON.TryLoad(out string encrypted, key))
            return false;
        try
        {
            string decrypted = encrypted.DecryptString();
            string[] parts = decrypted.Split(':');
            if (parts.Length != 2)
                throw new Exception("Corrupt score data");

            string data = parts[0];
            string hash = parts[1];
            if (hash != ComputeHash(data))
                throw new Exception("Score tampering detected!");

            long encoded = long.Parse(data);
            score = encoded.ToDouble();
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[SecureScore] Failed to load score: {ex.Message}");
            return false;
        }
    }
}
#endregion
#region Core Persistent JSON System
public static partial class PersistentJSON
{
    public static bool DebugMode => false;
    [System.Serializable]
    private class ListWrapper<TItem>
    {
        public List<TItem> Items;
        public ListWrapper() { }
        public ListWrapper(List<TItem> items)
        {
            Items = items;
        }
    }
    [System.Serializable]
    private class PrimitiveWrapper<T>
    {
        public T Value;
        public PrimitiveWrapper(T value) => Value = value;
    }
    private static string SaveFilePath<T>(string fileName)
    {
        string typeName = typeof(T).Name;
        if (typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() == typeof(List<>))
        {
            var elementType = typeof(T).GetGenericArguments()[0];
            typeName = $"ListOf_{elementType.Name}";
        }
        string safeFileName = fileName.Replace(" ", "_");
        return Path.Combine(Application.persistentDataPath, $"Json Storage/{safeFileName}_{typeName}.json");
    }
    public static bool TrySave<T>(T saveItem, string key)
    {
        if (saveItem == null) return false;
        string json;
        if (typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() == typeof(List<>))
        {
            var elementType = typeof(T).GetGenericArguments()[0];
            var wrapperType = typeof(ListWrapper<>).MakeGenericType(elementType);
            var wrapper = Activator.CreateInstance(wrapperType, saveItem);
            json = JsonUtility.ToJson(wrapper, true);
        }
        else if (IsPrimitiveOrString(typeof(T)))
        {
            var wrapper = new PrimitiveWrapper<T>(saveItem);
            json = JsonUtility.ToJson(wrapper, true);
        }
        else
        {
            json = JsonUtility.ToJson(saveItem, true);
        }
        string slotKey = GetSlotKey(key);
        if (IsWebGLBuild)
            return TrySaveWebGL(saveItem, slotKey, json);
        string path = GetSlotPath<T>(key);
        File.WriteAllText(path, json);
        if (DebugMode)
            Debug.Log($"Saved {typeof(T).Name} to {path}");
        return true;
    }
    public static bool TryLoad<T>(out T target, string key)
    {
        target = default(T);
        string json = null;
        string slotKey = GetSlotKey(key);
        if (IsWebGLBuild)
        {
            if (!TryLoadWebGL(out json, slotKey))
                return false;
        }
        else
        {
            string path = GetSlotPath<T>(key);
            if (!File.Exists(path))
            {
                if (DebugMode)
                    Debug.LogWarning($"No save found at {path}");
                return false;
            }
            json = File.ReadAllText(path);
        }
        T item;
        if (typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() == typeof(List<>))
        {
            var elementType = typeof(T).GetGenericArguments()[0];
            var wrapperType = typeof(ListWrapper<>).MakeGenericType(elementType);
            var wrapper = JsonUtility.FromJson(json, wrapperType);
            var itemsField = wrapperType.GetField("Items");
            item = (T)itemsField.GetValue(wrapper);
        }
        else if (IsPrimitiveOrString(typeof(T)))
        {
            var wrapper = JsonUtility.FromJson<PrimitiveWrapper<T>>(json);
            item = wrapper.Value;
        }
        else
        {
            item = JsonUtility.FromJson<T>(json);
        }
        if (item == null)
        {
            Debug.LogWarning($"Failed to deserialize {typeof(T).Name} from {(IsWebGLBuild ? "PlayerPrefs" : "file")}");
            return false;
        }
        target = item;
        if (DebugMode)
        {
            if (IsWebGLBuild)
                Debug.Log($"Loaded {typeof(T).Name} from PlayerPrefs key '{slotKey}'");
            else
                Debug.Log($"Loaded {typeof(T).Name} from file");
        }
        return true;
    }
    private static bool IsPrimitiveOrString(Type t)
    {
        return t.IsPrimitive || t == typeof(string) ||
               t == typeof(decimal) || t == typeof(double) ||
               t == typeof(float);
    }
}
#endregion
#region Save Slot Management
public static partial class PersistentJSON
{
    private static int _currentSlot = 0;
    public static int CurrentSlot
    {
        get => _currentSlot;
        set
        {
            if (value < 0)
            {
                Debug.LogWarning("[PersistentJSON] Slot index cannot be negative. Defaulting to 0.");
                _currentSlot = 0;
            }
            else
            {
                _currentSlot = value;
                if (DebugMode)
                    Debug.Log($"[PersistentJSON] Switched to save slot {_currentSlot}");
            }
        }
    }
    private static string GetSlotKey(string baseKey)
    {
        return $"{baseKey}_slot{_currentSlot}";
    }
    private static string GetSlotPath<T>(string baseKey)
    {
        string slotFolder = Path.Combine(Application.persistentDataPath, "Json Storage", $"Slot_{_currentSlot}");
        Directory.CreateDirectory(slotFolder);
        string typeName = typeof(T).Name;
        if (typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() == typeof(List<>))
        {
            var elementType = typeof(T).GetGenericArguments()[0];
            typeName = $"ListOf_{elementType.Name}";
        }
        string safeFileName = baseKey.Replace(" ", "_");
        return Path.Combine(slotFolder, $"{safeFileName}_{typeName}.json");
    }
    public static void ClearSlot()
    {
        if (IsWebGLBuild)
        {
            Debug.LogWarning("[PersistentJSON] ClearSlot() on WebGL only works for known keys you manually delete.");
            return;
        }
        string slotFolder = Path.Combine(Application.persistentDataPath, "Json Storage", $"Slot_{_currentSlot}");
        if (Directory.Exists(slotFolder))
        {
            Directory.Delete(slotFolder, true);
            if (DebugMode)
                Debug.Log($"[PersistentJSON] Cleared slot folder: {slotFolder}");
        }
        else if (DebugMode)
        {
            Debug.Log($"[PersistentJSON] No folder found for slot {_currentSlot}");
        }
    }
}
#endregion