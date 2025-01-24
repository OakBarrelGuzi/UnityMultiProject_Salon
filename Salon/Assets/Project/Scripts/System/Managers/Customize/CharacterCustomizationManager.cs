using System;
using System.Collections.Generic;
using UI;
using UnityEngine;
using Salon.Firebase;
using Salon.Firebase.Database;
using System.Threading.Tasks;

namespace Character
{
    public class CharacterCustomizationManager : MonoBehaviour
    {
        public event Action<Dictionary<string, string>> OnCustomizationChanged;

        public CharacterCustomizationCategory[] categories;

        public CharacterCustomization customization;

        public Animator animator;

        [ContextMenu("Set Names")]
        void SetNames()
        {
            foreach (CharacterCustomizationCategory category in categories)
            {
                int optionCounter = 1;
                foreach (CharacterCustomizationOption option in category.options)
                {
                    option.name = option.model.name.Replace("_", " ");
                    option.id = category.name + "_" + optionCounter;
                    option.description = option.model.name.Replace("_", " ");
                    optionCounter++;
                }
            }
        }

        [ContextMenu("Set Icons")]
        void SetIcons()
        {
            foreach (CharacterCustomizationCategory category in categories)
            {
                foreach (CharacterCustomizationOption option in category.options)
                {
                    option.icon = Resources.Load<Sprite>("Sprites/" + option.model.name);
                }
            }
        }

        private async void Start()
        {
            customization = new CharacterCustomization();

            // 현재 씬 이름 가져오기
            string currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

            if (currentSceneName == "CustomizeScene")
            {
                await InitializeForCustomizeScene();
            }
            else
            {
                // 채널/룸 씬에서는 RoomManager가 데이터를 전달할 때까지 대기
                foreach (CharacterCustomizationCategory category in categories)
                {
                    foreach (CharacterCustomizationOption option in category.options)
                    {
                        option.model.SetActive(false);
                    }
                }
            }
        }

        private async Task InitializeForCustomizeScene()
        {
            // FirebaseManager가 초기화될 때까지 대기
            while (!FirebaseManager.Instance.IsInitialized)
            {
                await Task.Delay(100);
            }

            await LoadCustomizationFromFirebase();

            foreach (CharacterCustomizationCategory category in categories)
            {
                foreach (CharacterCustomizationOption option in category.options)
                {
                    option.model.SetActive(false);
                }
            }

            ApplyCustomization();
            UI_CusomizationManager.Instance.Initialize(this);
        }

        // 채널/룸 씬에서 사용할 메서드
        public void ApplyCustomizationData(Dictionary<string, string> customizationData)
        {
            if (customizationData == null) return;

            customization.selectedOptions = new Dictionary<string, string>(customizationData);
            ApplyCustomization();
            Debug.Log("커스터마이제이션 데이터가 적용되었습니다.");
        }

        private async Task LoadCustomizationFromFirebase()
        {
            try
            {
                if (!FirebaseManager.Instance.IsInitialized)
                {
                    Debug.LogWarning("Firebase가 아직 초기화되지 않았습니다. 기본값을 설정합니다.");
                    SetDefault();
                    return;
                }

                var userData = await FirebaseManager.Instance.GetUserData();
                if (userData != null && userData.CharacterCustomization != null &&
                    userData.CharacterCustomization.selectedOptions != null &&
                    userData.CharacterCustomization.selectedOptions.Count > 0)
                {
                    customization.selectedOptions = userData.CharacterCustomization.selectedOptions;
                    Debug.Log("커스터마이제이션 데이터를 Firebase에서 로드했습니다.");
                }
                else
                {
                    Debug.Log("Firebase에 저장된 커스터마이제이션 데이터가 없어 기본값을 설정합니다.");
                    SetDefault();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"커스터마이제이션 로드 실패: {e.Message}");
                SetDefault();
            }
        }

        private async void SaveCustomizationToFirebase()
        {
            // CustomizeScene에서만 저장 가능하도록 체크
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "CustomizeScene")
            {
                return;
            }

            try
            {
                if (!FirebaseManager.Instance.IsInitialized)
                {
                    Debug.LogWarning("Firebase가 아직 초기화되지 않아 커스터마이제이션을 저장할 수 없습니다.");
                    return;
                }

                if (string.IsNullOrEmpty(FirebaseManager.Instance.CurrentUserUID))
                {
                    Debug.LogWarning("현재 로그인된 사용자가 없어 커스터마이제이션을 저장할 수 없습니다.");
                    return;
                }

                var userData = await FirebaseManager.Instance.GetUserData();
                if (userData == null)
                {
                    Debug.LogWarning("사용자 데이터를 가져올 수 없어 커스터마이제이션을 저장할 수 없습니다.");
                    return;
                }

                if (userData.CharacterCustomization == null)
                {
                    userData.CharacterCustomization = new CharacterCustomizationData();
                }
                userData.CharacterCustomization.selectedOptions = customization.selectedOptions;

                // 한 번 더 UID 체크
                if (!string.IsNullOrEmpty(FirebaseManager.Instance.CurrentUserUID))
                {
                    await FirebaseManager.Instance.UpdateUserData(userData);
                    Debug.Log("커스터마이제이션 데이터를 Firebase에 저장했습니다.");
                }
                else
                {
                    Debug.LogWarning("저장 직전 사용자 연결이 끊어져 커스터마이제이션을 저장할 수 없습니다.");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"커스터마이제이션 저장 실패: {e.Message}");
            }
        }

        public void SelectOption(string categoryID, string optionID)
        {
            if (customization.selectedOptions.ContainsKey(categoryID))
            {
                customization.selectedOptions[categoryID] = optionID;
            }
            else
            {
                customization.selectedOptions.Add(categoryID, optionID);
            }

            ApplyCustomization();
            OnCustomizationChanged?.Invoke(customization.selectedOptions);
            SaveCustomizationToFirebase();
        }

        [ContextMenu("Apply Customization")]
        public void ApplyCustomization()
        {
            foreach (CharacterCustomizationCategory category in categories)
            {
                foreach (CharacterCustomizationOption option in category.options)
                {
                    option.model.SetActive(false);
                }
            }

            foreach (CharacterCustomizationCategory category in categories)
            {
                if (customization.selectedOptions.ContainsKey(category.id))
                {
                    string optionID = customization.selectedOptions[category.id];
                    foreach (CharacterCustomizationOption option in category.options)
                    {
                        if (option.id == optionID)
                        {
                            option.model.SetActive(true);
                        }
                    }
                }
            }
        }

        void SetDefault()
        {
            foreach (var category in categories)
            {
                SelectOption(category.id, category.options[0].id);
            }
        }

        public void PlayAnimation()
        {
            animator.SetTrigger("Play");
        }

        [ContextMenu("Save Current Setup To SO")]
        private void SaveCurrentSetupToSO()
        {
#if UNITY_EDITOR
            var so = ScriptableObject.CreateInstance<CharacterCustomizationSO>();
            so.categories = new CharacterCustomizationSO.CharacterCustomizationCategory[categories.Length];

            // 현재 설정을 SO 형식으로 변환
            for (int i = 0; i < categories.Length; i++)
            {
                var currentCategory = categories[i];
                var newCategory = new CharacterCustomizationSO.CharacterCustomizationCategory
                {
                    name = currentCategory.name,
                    id = currentCategory.id,
                    description = currentCategory.description,
                    options = new CharacterCustomizationSO.CharacterCustomizationOption[currentCategory.options.Length]
                };

                for (int j = 0; j < currentCategory.options.Length; j++)
                {
                    var currentOption = currentCategory.options[j];
                    newCategory.options[j] = new CharacterCustomizationSO.CharacterCustomizationOption
                    {
                        name = currentOption.name,
                        id = currentOption.id,
                        description = currentOption.description,
                        icon = currentOption.icon,
                        model = currentOption.model
                    };
                }

                so.categories[i] = newCategory;
            }

            string path = UnityEditor.EditorUtility.SaveFilePanelInProject(
                "Save Current Setup",
                "CustomizationSetup",
                "asset",
                "Save current customization setup to a ScriptableObject"
            );

            if (!string.IsNullOrEmpty(path))
            {
                UnityEditor.AssetDatabase.CreateAsset(so, path);
                UnityEditor.AssetDatabase.SaveAssets();
                UnityEditor.AssetDatabase.Refresh();
                Debug.Log($"현재 커스터마이제이션 설정이 저장되었습니다: {path}");
            }
#endif
        }

        [ContextMenu("Load Setup From SO")]
        private void LoadSetupFromSO()
        {
#if UNITY_EDITOR
            string path = UnityEditor.EditorUtility.OpenFilePanel(
                "Load Customization Setup",
                "Assets",
                "asset"
            );

            if (!string.IsNullOrEmpty(path))
            {
                string projectPath = System.IO.Path.GetFullPath("Assets").Replace("\\", "/");
                string relativePath = "Assets" + path.Replace(projectPath, "");

                var so = UnityEditor.AssetDatabase.LoadAssetAtPath<CharacterCustomizationSO>(relativePath);
                if (so != null)
                {
                    categories = new CharacterCustomizationCategory[so.categories.Length];
                    for (int i = 0; i < so.categories.Length; i++)
                    {
                        var soCategory = so.categories[i];
                        var newCategory = new CharacterCustomizationCategory
                        {
                            name = soCategory.name,
                            id = soCategory.id,
                            description = soCategory.description,
                            options = new CharacterCustomizationOption[soCategory.options.Length]
                        };

                        for (int j = 0; j < soCategory.options.Length; j++)
                        {
                            var soOption = soCategory.options[j];
                            newCategory.options[j] = new CharacterCustomizationOption
                            {
                                name = soOption.name,
                                id = soOption.id,
                                description = soOption.description,
                                icon = soOption.icon,
                                model = soOption.model
                            };
                        }

                        categories[i] = newCategory;
                    }

                    Debug.Log($"커스터마이제이션 설정을 불러왔습니다: {relativePath}");
                    SetDefault();
                }
            }
#endif
        }

        [ContextMenu("Auto Set Model References")]
        private void AutoSetModelReferences()
        {
            foreach (var category in categories)
            {
                foreach (var option in category.options)
                {
                    if (option.model == null && !string.IsNullOrEmpty(option.name))
                    {
                        string searchName = option.name.Replace(" ", "_");

                        Transform foundChild = transform.Find(searchName);
                        if (foundChild != null)
                        {
                            option.model = foundChild.gameObject;
                            Debug.Log($"모델 참조 설정 완료: {option.name} -> {foundChild.name}");
                        }
                        else
                        {
                            Debug.LogWarning($"모델을 찾을 수 없습니다: {searchName}");
                        }
                    }
                }
            }
        }
    }

    [Serializable]
    public class CharacterCustomization
    {
        public Dictionary<string, string> selectedOptions;

        public CharacterCustomization()
        {
            selectedOptions = new Dictionary<string, string>();
        }

        void ToJson()
        {
            string json = JsonUtility.ToJson(this);
            Debug.Log(json);
        }

        void FromJson(string json)
        {
            CharacterCustomization customization = JsonUtility.FromJson<CharacterCustomization>(json);
            selectedOptions = customization.selectedOptions;
        }
    }

    [Serializable]
    public class Data
    {
        public string name;
        public string id;
        public string description;
    }

    [Serializable]
    public class CharacterCustomizationCategory : Data
    {
        public CharacterCustomizationOption[] options;
    }

    [Serializable]
    public class CharacterCustomizationOption : Data
    {
        public Sprite icon;
        public GameObject model;
    }
}