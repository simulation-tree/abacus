namespace Abacus.Manager
{
    public static class LoadingAnimation
    {
        public static readonly string[] frames = [
            "|",
            "/",
            "-",
            "\\"
        ];

        public static string GetFrame(int index)
        {
            index = index % frames.Length;
            return frames[index].ToString();
        }
    }
}