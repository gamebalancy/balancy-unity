namespace Balancy
{
    public static class Main
    {
        public static void Init(AppConfig config)
        {
            DataEditor.Init();
            PrepareOtherPlugins();
            Controller.Init(config);
        }

        private static void PrepareOtherPlugins()
        {
            
        }
    }
}