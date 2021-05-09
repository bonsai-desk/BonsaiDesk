namespace Bonsai.Web
{
    public interface IBonsaiWebServer
    {
        void Start();

        void Shutdown();

        void Pause();

        void Resume();
    }
}