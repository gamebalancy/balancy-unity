using UnityEngine;

namespace Balancy
{
    public abstract class LocalizationUI : MonoBehaviour
    {
#pragma warning disable 649
        [SerializeField]
        private string localizationKey;
#pragma warning restore 649

        protected abstract void OnLocalizationChanged(string localizationCode);

        protected virtual void Start()
        {
            Balancy.Localization.Manager.SubscribeOnLocalizationChanged(OnLocalizationChanged);
        }

        protected virtual void OnDestroy()
        {
            Balancy.Localization.Manager.UnsubscribeOnLocalizationChanged(OnLocalizationChanged);
        }

        protected string GetLocalization()
        {
            return Balancy.Localization.Manager.Get(localizationKey);
        }
    }
}