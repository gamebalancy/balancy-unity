using UnityEngine;
using UnityEngine.UI;

namespace Balancy
{
    [RequireComponent(typeof(Text))]
    public class LocalizationText : LocalizationUI
    {
        private Text _component;

        protected override void Start()
        {
            base.Start();
            _component = GetComponent<Text>();
            SetTextValue();
        }

        protected override void OnLocalizationChanged(string localizationCode)
        {
            SetTextValue();
        }

        void SetTextValue()
        {
            if (_component != null)
                _component.text = GetLocalization();
        }
    }
}