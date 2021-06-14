#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using Balancy.Dictionaries;
using Balancy.Editor;

public class Balancy_Plugins
{
    private const string PLUGINS_LOCAL_FOLDER = "Assets/Balancy/";
    private const string PLUGINS_ADDRESS_LOCAL = "Assets/Balancy/Editor/plugins.json";
    private const string PLUGINS_ADDRESS_ORIGINAL = "Assets/Balancy/Editor/balancy_plugins.json";
#if LOCAL_PLUGINS_TEST
    private const string PLUGINS_ADDRESS_REMOTE = "Assets/balancy_plugins_remote.json";
#else
    private const string PLUGINS_ADDRESS_REMOTE = "https://dictionaries-unnynet.fra1.cdn.digitaloceanspaces.com/config/balancy_plugins.json";
#endif
    
    
    
    private const string CODE_GENERATION_PLUGIN = "Balancy";
    private const string CODE_GENERATION_FILE = "Assets/Balancy/Scripts/BalancyMain.cs";

    private static readonly GUIStyle LABEL_WRAP = new GUIStyle(GUI.skin.GetStyle("label")) {wordWrap = true};
    private static readonly GUIStyle LABEL_BOLD = new GUIStyle(GUI.skin.GetStyle("label")) {fontStyle = FontStyle.Bold};
    
    private static readonly GUILayoutOption LAYOUT_NAME = GUILayout.Width(100);
    private static readonly GUILayoutOption LAYOUT_VERSION = GUILayout.Width(50);
    private static readonly GUILayoutOption LAYOUT_BUTTON = GUILayout.Width(100);
    private static readonly GUILayoutOption LAYOUT_BUTTON_REFRESH = GUILayout.Width(30);
    
    private static Action<PluginInfo> onUpdatePluginInfo;
    private static Action<PluginInfo> onRemovePluginInfo;
    private static Action onRedraw;

    enum Status
    {
        None,
        Downloading,
        Ready
    }
    
    private class PluginsFile
    {
#pragma warning disable 649
        [JsonProperty("editor")]
        public EditorInfo EditorInfo;
        
        [JsonProperty("plugins")]
        public List<PluginInfo> Plugins;
#pragma warning restore 649
        
        public PluginInfo GetOrCreatePluginInfo(string name)
        {
            var info = GetPluginInfo(name);
            if (info != null)
                return info;
            
            info = new PluginInfo();
            Plugins.Add(info);

            info.Name = name; 

            return info;
        }
        
        public PluginInfo GetPluginInfo(string name)
        {
            foreach (var plugin in Plugins)
            {
                if (string.Equals(plugin.Name, name))
                    return plugin;
            }

            return null;
        }

        public void UpdatePluginInfo(PluginInfo pluginInfo)
        {
            RemovePluginInfo(pluginInfo);
            Plugins.Add(pluginInfo);
        }
        
        public void RemovePluginInfo(PluginInfo pluginInfo)
        {
            for (int i = 0;i<Plugins.Count;i++)
            {
                if (string.Equals(Plugins[i].Name, pluginInfo.Name))
                {
                    Plugins.RemoveAt(i);
                    break;
                }
            }
        }
    }

#pragma warning disable 649
    private class EditorButton
    {
        [JsonProperty("title")]
        public string title;
        
        [JsonProperty("url")]
        public string Url;
    }
    
    private class EditorMessage
    {
        [JsonProperty("text")]
        public string Text;
        
        [JsonProperty("buttons")]
        public EditorButton[] Buttons;
        
        [JsonProperty("min_version")]
        private string _minVersionString;
        
        [JsonProperty("max_version")]
        private string _maxVersionString;
        
        [JsonIgnore]
        private PluginVersion _minVersion;
        
        [JsonIgnore]
        private PluginVersion _maxVersion;
        
        [JsonIgnore]
        public PluginVersion MinVersion
        {
            get
            {
                if (_minVersion == null && !string.IsNullOrEmpty(_minVersionString))
                    _minVersion = new PluginVersion(_minVersionString);
                return _minVersion;
            }
        }
        
        [JsonIgnore]
        public PluginVersion MaxVersion
        {
            get
            {
                if (_maxVersion == null && !string.IsNullOrEmpty(_maxVersionString))
                    _maxVersion = new PluginVersion(_maxVersionString);
                return _maxVersion;
            }
        }
    }
    
    private class EditorInfo
    {
        [JsonProperty("url")]
        public string DownloadUrl;
        
        [JsonProperty("version")]
        private string _versionString;
        
        [JsonProperty("min_version")]
        private string _minVersionString;
        
        [JsonProperty("message")]
        public EditorMessage Message;
        
        [JsonIgnore]
        private PluginVersion _version;
        [JsonIgnore]
        private PluginVersion _minVersion;
        
        [JsonIgnore]
        public PluginVersion Version
        {
            get
            {
                if (_version == null && !string.IsNullOrEmpty(_versionString))
                    _version = new PluginVersion(_versionString);
                return _version;
            }
        }
        
        [JsonIgnore]
        public PluginVersion MinVersion
        {
            get
            {
                if (_minVersion == null && !string.IsNullOrEmpty(_minVersionString))
                    _minVersion = new PluginVersion(_minVersionString);
                return _minVersion;
            }
        }
    }
    
    private class PluginInfoBase
    {
        [JsonProperty("name")]
        public string Name;
        [JsonProperty("version")]
        private string _versionString;

        [JsonIgnore]
        private PluginVersion _version;

        [JsonIgnore]
        public PluginVersion Version
        {
            get
            {
                if (_version == null && !string.IsNullOrEmpty(_versionString))
                    _version = new PluginVersion(_versionString);
                return _version;
            }
        }
    }

    private class PluginVersion
    {
        private readonly int Major;
        private readonly int Minor;
        private readonly int Patch;

        public PluginVersion(string version)
        {
            var items = version.Split('.');
            if (items.Length > 0)
            {
                int.TryParse(items[0], out Major);

                if (items.Length > 1)
                {
                    int.TryParse(items[1], out Minor);
                    
                    if (items.Length > 2)
                        int.TryParse(items[2], out Patch);
                }
            }
        }
        public bool IsHigherOrEqualThan(PluginVersion otherVersion)
        {
            if (otherVersion.Major != Major)
                return Major > otherVersion.Major;
            
            if (otherVersion.Minor != Minor)
                return Minor > otherVersion.Minor;

            return Patch >= otherVersion.Patch;
        }

        public new string ToString()
        {
            return string.Format("{0}.{1}.{2}", Major, Minor, Patch);
        }
    }
#pragma warning restore 649

    private class PluginInfo : PluginInfoBase
    {
#pragma warning disable 649
        [JsonProperty("description")]
        private string Description;
        [JsonProperty("can_be_removed")]
        private bool CanBeRemoved;
        [JsonProperty("download")]
        private DownloadInfo[] Download;
        [JsonProperty("dependencies")]
        public PluginInfoBase[] Dependencies;
        [JsonProperty("code")]
        public string Code;
        [JsonProperty("documentation")]
        private string Documentation;
#pragma warning restore 649

        [JsonIgnore]
        public bool Installing;
        [JsonIgnore]
        private float InstallProgress;

        public void Render(PluginInfo localInfo, Func<PluginInfo, bool> canInstall, Func<PluginInfo, bool> canRemove)
        {
            var localVersion = localInfo.Version;
            
            GUILayout.BeginHorizontal(EditorStyles.helpBox);
            GUILayout.Label( new GUIContent
            {
                text = Name,
                tooltip = Description
            }, LABEL_BOLD, LAYOUT_NAME);
            GUILayout.Label(localVersion == null ? string.Empty : "v" + localVersion.ToString(), LAYOUT_VERSION);

            if (Installing)
            {
                GUI.enabled = true;
                var rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
                EditorGUI.ProgressBar(rect, InstallProgress, "Installing v" + Version.ToString());
                GUI.enabled = false;
            }
            else
            {
                if (localVersion != null && localVersion.IsHigherOrEqualThan(Version))
                    GUILayout.Label("Up to date");
                
                GUILayout.FlexibleSpace();
                
                if (localVersion == null || !localVersion.IsHigherOrEqualThan(Version))
                {
                    if (localVersion == null)
                    {
                        if (GUILayout.Button("Install v" + Version.ToString(), LAYOUT_BUTTON))
                        {
                            if (canInstall(this))
                                InstallPlugin();
                        }
                    }
                    else
                    {
                        if (GUILayout.Button("Update v" + Version.ToString(), LAYOUT_BUTTON))
                        {
                            if (canInstall(this))
                                UpdatePlugin(localInfo);
                        }
                    }
                }

                if (CanBeRemoved && localVersion != null)
                {
                    if (GUILayout.Button(EditorGUIUtility.IconContent("d_SVN_DeletedLocal"), LAYOUT_BUTTON_REFRESH))
                    {
                        if (canRemove(this))
                            localInfo.RemovePlugin();
                    }
                }

                if (!string.IsNullOrEmpty(Documentation))
                {
                    if (GUILayout.Button(EditorGUIUtility.IconContent("_Help"), LAYOUT_BUTTON_REFRESH))
                        Application.OpenURL(Documentation);
                }
            }

            GUILayout.EndHorizontal();
        }

        private void RemovePlugin()
        {
            for (int i = 0; i < Download.Length; i++)
                FileHelper.DeleteFileAtPath(PLUGINS_LOCAL_FOLDER + Download[i].File);
            AssetDatabase.Refresh();

            onRemovePluginInfo(this);
        }
        
        private void InstallPlugin()
        {
            EditorCoroutineHelper.Execute(InstallAllFiles());
        }

        IEnumerator InstallAllFiles()
        {
            Installing = true;

            byte[][] remoteFiles = new byte[Download.Length][];

            var perFileProgress = 1f / Download.Length;

            for (int i = 0; i < Download.Length; i++)
            {
                UnityWebRequest www = UnityWebRequest.Get(Download[i].Url);
                yield return www.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
                if (www.result != UnityWebRequest.Result.Success)
#else
                if (www.isNetworkError || www.isHttpError)
#endif
                {
                    EditorUtility.DisplayDialog("Error", www.error, "Ok");
                    yield break;
                }

                remoteFiles[i] = www.downloadHandler.data;

                InstallProgress = (i + 1) * perFileProgress;
                onRedraw();
            }

            for (int i = 0; i < Download.Length; i++)
            {
                var fullPath = PLUGINS_LOCAL_FOLDER + Download[i].File;
                Balancy.Utils.CheckAndCreateDirectoryForFile(fullPath);
                File.WriteAllBytes(fullPath, remoteFiles[i]);
            }

            Installing = false;
            onUpdatePluginInfo(this);
            AssetDatabase.Refresh();
        }

        private void UpdatePlugin(PluginInfo localInfo)
        {
            localInfo.RemovePlugin();
            InstallPlugin();
        }
    }

#pragma warning disable 649
    private class DownloadInfo
    {
        [JsonProperty("url")]
        public string Url;
        [JsonProperty("file")]
        public string File;
    }
#pragma warning restore 649
    
    private PluginsFile _pluginsLocal;
    private PluginsFile _pluginsOriginal;
    private PluginsFile _pluginsRemote;
    private Status _status;
    
    public Balancy_Plugins(EditorWindow parent)
    {
        onUpdatePluginInfo = UpdateLocalPluginInfo;
        onRemovePluginInfo = RemoveLocalPluginInfo;
        onRedraw = parent.Repaint;
        
        Refresh();
    }

    private void UpdateLocalPluginInfo(PluginInfo pluginInfo)
    {
        _pluginsLocal.UpdatePluginInfo(pluginInfo);
        SynchLocalPluginsInfo();
    }
    
    private void RemoveLocalPluginInfo(PluginInfo pluginInfo)
    {
        _pluginsLocal.RemovePluginInfo(pluginInfo);
        SynchLocalPluginsInfo();
    }

    private void SynchLocalPluginsInfo()
    {
        onRedraw();
        SaveLocalFile();
        GenerateCodeForStartFile();
    }

    private void SaveLocalFile()
    {
        string str = JsonConvert.SerializeObject(_pluginsLocal);
        Balancy.Utils.CheckAndCreateDirectoryForFile(PLUGINS_ADDRESS_LOCAL);
        FileHelper.SaveToFilePath(PLUGINS_ADDRESS_LOCAL, str);
        AssetDatabase.Refresh();
    }

    private void GenerateCodeForStartFile()
    {
        var mainPlugin = _pluginsRemote.GetPluginInfo(CODE_GENERATION_PLUGIN);

        string insertString = string.Empty;
        for (int i = 0; i < _pluginsLocal.Plugins.Count; i++)
        {
            var plugin = _pluginsLocal.Plugins[i];
            if (plugin.Version == null || string.Equals(plugin.Name, CODE_GENERATION_PLUGIN))
                continue;

            insertString += plugin.Code;
            if (i != _pluginsLocal.Plugins.Count - 1)
                insertString += "\n\t\t\t";
        }

        const string replace = "{0}";

        string code = mainPlugin.Code.Replace(replace, insertString);
        Balancy.Utils.CheckAndCreateDirectoryForFile(CODE_GENERATION_FILE);
        FileHelper.SaveToFilePath(CODE_GENERATION_FILE, code);
        AssetDatabase.Refresh();
    }
    
    public void Render()
    {
        GUI.enabled = true;
        
        GUILayout.BeginVertical(EditorStyles.helpBox);

        RenderHeader();

        if (_pluginsRemote == null)
        {
            if (_status == Status.Ready)
                GUILayout.Label("Something went wrong. Please try to refresh the page.");
            else
                GUILayout.Label("Updating...");
        }
        else
            RenderAllPlugins();

        GUILayout.EndVertical();
    }

    private void RenderHeader()
    {
        GUILayout.BeginHorizontal(EditorStyles.label);
        GUILayout.Label("Additional Plugins");
        GUI.enabled = _status != Status.Downloading;
        if (GUILayout.Button(EditorGUIUtility.IconContent("Refresh"), LAYOUT_BUTTON_REFRESH)) 
            Refresh();
        GUI.enabled = true;
        GUILayout.EndHorizontal();
    }

    private bool IsAnythingInstalling()
    {
        foreach (var plugin in _pluginsRemote.Plugins)
            if (plugin.Installing)
                return true;
        return false;
    }

    private void RenderAllPlugins()
    {
        if (_pluginsLocal.EditorInfo.Version.IsHigherOrEqualThan(_pluginsRemote.EditorInfo.MinVersion))
        {
            if (!_pluginsOriginal.EditorInfo.Version.IsHigherOrEqualThan(_pluginsRemote.EditorInfo.Version))
                RenderNormalEditorUpdate();

            RenderEditorUpdateMessage();
            
            GUI.enabled = !IsAnythingInstalling();
            foreach (var plugin in _pluginsRemote.Plugins)
            {
                var local = _pluginsLocal.GetOrCreatePluginInfo(plugin.Name);
                plugin.Render(local, CanInstall, CanRemove);
            }
        }
        else
            RenderForceEditorUpdate();
    }
    
    private void RenderEditorUpdateMessage()
    {
        if (_pluginsRemote.EditorInfo.Message == null || string.IsNullOrEmpty(_pluginsRemote.EditorInfo.Message.Text))
            return;

        if (!_pluginsLocal.EditorInfo.Version.IsHigherOrEqualThan(_pluginsRemote.EditorInfo.Message.MinVersion) ||
            !_pluginsRemote.EditorInfo.Message.MaxVersion.IsHigherOrEqualThan(_pluginsLocal.EditorInfo.Version)) return;
        
        GUI.color = Color.cyan;
        GUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.Label(_pluginsRemote.EditorInfo.Message.Text, LABEL_WRAP);
        GUI.color = Color.white;

        if (_pluginsRemote.EditorInfo.Message.Buttons != null)
        {
            GUILayout.BeginHorizontal(EditorStyles.label);
            foreach (var btn in _pluginsRemote.EditorInfo.Message.Buttons)
            {
                if (GUILayout.Button(btn.title, LAYOUT_BUTTON))
                    Application.OpenURL(btn.Url);
            }

            GUILayout.EndHorizontal();
        }

        GUILayout.EndVertical();
    }
    
    private void RenderNormalEditorUpdate()
    {
        GUI.color = Color.green;
        GUILayout.BeginHorizontal(EditorStyles.helpBox);
        GUILayout.Label("New version of Balancy plugin is now available! Hooray!");
        GUI.color = Color.white;
        if (GUILayout.Button("Update v" + _pluginsRemote.EditorInfo.Version.ToString(), LAYOUT_BUTTON))
            Application.OpenURL(_pluginsRemote.EditorInfo.DownloadUrl);
        GUILayout.EndHorizontal();
    }
    
    private void RenderForceEditorUpdate()
    {
        GUI.color = Color.red;
        GUILayout.BeginHorizontal(EditorStyles.helpBox);
        GUILayout.Label("Please update Balancy plugin to the newest version!");
        GUI.color = Color.white;
        if (GUILayout.Button("Update v" + _pluginsRemote.EditorInfo.Version.ToString(), LAYOUT_BUTTON))
            Application.OpenURL(_pluginsRemote.EditorInfo.DownloadUrl);
        GUILayout.EndHorizontal();
    }

    private bool CanInstall(PluginInfo pluginInfo)
    {
        var deps = GetListOfMissingDependencies(pluginInfo);

         if (string.IsNullOrEmpty(deps))
             return true;
         
         EditorUtility.DisplayDialog("Warning", deps, "Ok");
         return false;
    }
    
    private bool CanRemove(PluginInfo pluginInfo)
    {
        var deps = GetListOfInversedMissingDependencies(pluginInfo);

        if (string.IsNullOrEmpty(deps))
            return true;
         
        EditorUtility.DisplayDialog("Warning", deps, "Ok");
        return false;
    }

    private string GetListOfMissingDependencies(PluginInfo pluginInfo)
    {
        List<PluginInfoBase> missingDeps = new List<PluginInfoBase>();
        foreach (var dep in pluginInfo.Dependencies)
        {
            var localPlugin = _pluginsLocal.GetPluginInfo(dep.Name);
            if (localPlugin?.Version == null || !localPlugin.Version.IsHigherOrEqualThan(dep.Version))
                missingDeps.Add(dep);
        }

        if (missingDeps.Count == 0)
            return null;

        string depStr = "Please Install the next plugins first:\n";
        foreach (var dep in missingDeps)
            depStr += string.Format("{0} : v{1}\n", dep.Name, dep.Version.ToString());

        return depStr;
    }
    
    private string GetListOfInversedMissingDependencies(PluginInfo pluginInfo)
    {
        List<PluginInfoBase> missingDeps = new List<PluginInfoBase>();
        foreach (var plugin in _pluginsLocal.Plugins)
        {
            if (plugin.Dependencies == null)
                continue;
            
            foreach (var dep in plugin.Dependencies)
            {
                if (string.Equals(pluginInfo.Name, dep.Name))
                    missingDeps.Add(plugin);
            }
        }

        if (missingDeps.Count == 0)
            return null;

        string depStr = "Other plugins depend on this one:\n";
        foreach (var dep in missingDeps)
            depStr += string.Format("{0} : v{1}\n", dep.Name, dep.Version.ToString());

        return depStr;
    }

    private void Refresh()
    {
        _status = Status.Downloading;
        _pluginsRemote = null;

        _pluginsLocal = GetLocalPlugins(PLUGINS_ADDRESS_LOCAL);
        _pluginsOriginal = GetLocalPlugins(PLUGINS_ADDRESS_ORIGINAL);

        if (_pluginsLocal == null)
        {
            _pluginsLocal = GetLocalPlugins(PLUGINS_ADDRESS_ORIGINAL);
            SaveLocalFile();
        }
        else
            SynchVersionsFromOriginalFile();
        
#if LOCAL_PLUGINS_TEST
        _pluginsRemote = GetLocalPlugins(PLUGINS_ADDRESS_REMOTE);
        _status = Status.Ready;
#else
        EditorCoroutineHelper.Execute(LoadRemoteConfig());
#endif
    }

    private void SynchVersionsFromOriginalFile()
    {
        _pluginsLocal.EditorInfo = _pluginsOriginal.EditorInfo;
        for (int i = 0; i < _pluginsLocal.Plugins.Count; i++)
        {
            var local = _pluginsLocal.Plugins[i];
            var original = _pluginsOriginal.GetPluginInfo(local.Name);
            if (original != null)
            {
                if (original.Version.IsHigherOrEqualThan(local.Version))
                    _pluginsLocal.Plugins[i] = original;
            }
        }
    }

    private IEnumerator LoadRemoteConfig()
    {
        var r = new WWWResult();
        yield return Loader.TryLoadFile(PLUGINS_ADDRESS_REMOTE, r);
        if (!r.Success)
        {
            EditorUtility.DisplayDialog("Error", r.GetFullError(), "Ok");
            yield break;
        }
        
        var text = r.GetResult();
        _pluginsRemote = JsonConvert.DeserializeObject<PluginsFile>(text);
        
        _status = Status.Ready;
    }

    private PluginsFile GetLocalPlugins(string path)
    {
        TextAsset textAsset = (TextAsset)AssetDatabase.LoadAssetAtPath(path, typeof(TextAsset));
        return textAsset == null ? null : JsonConvert.DeserializeObject<PluginsFile>(textAsset.text);
    }
}
#endif