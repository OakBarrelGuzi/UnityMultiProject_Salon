using UnityEngine;
using System;
using System.Collections.Generic;

namespace Character
{
    [CreateAssetMenu(fileName = "CharacterCustomizationData", menuName = "Salon/Character/CustomizationData")]
    public class CharacterCustomizationSO : ScriptableObject
    {
        public CharacterCustomizationCategory[] categories;

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

        [Serializable]
        public class Data
        {
            public string name;
            public string id;
            public string description;
        }

        private void OnValidate()
        {
            // 에디터에서 데이터 유효성 검사
            foreach (var category in categories)
            {
                if (string.IsNullOrEmpty(category.id))
                {
                    category.id = category.name.Replace(" ", "_").ToLower();
                }

                int optionCounter = 1;
                foreach (var option in category.options)
                {
                    if (string.IsNullOrEmpty(option.id))
                    {
                        option.id = $"{category.id}_{optionCounter}";
                    }
                    if (string.IsNullOrEmpty(option.name) && option.model != null)
                    {
                        option.name = option.model.name.Replace("_", " ");
                    }
                    if (string.IsNullOrEmpty(option.description) && option.model != null)
                    {
                        option.description = option.model.name.Replace("_", " ");
                    }
                    optionCounter++;
                }
            }
        }
    }
}