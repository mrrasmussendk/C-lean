using System.IO;

namespace Fixture;

public static class Code
{
    public static bool Remove(string path)
    {
        try
        {
            File.Delete(path);
            return true;
        }
        catch (IOException)
        {
            return false;
        }
    }
}
