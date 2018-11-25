using System.IO;
using System;

public static class StreamHelper
{
    public static string ReadFile(string filePath)
    {
        // Stream data;
        string dataInFile = "";

        try
        {
            using (var stream = new FileStream(filePath, FileMode.Open))
            {
                using (var sr = new StreamReader(stream))
                {
                    dataInFile = sr.ReadToEnd();
                }
            }
        }
        catch (IOException)
        {
            return dataInFile;
        }
        catch (Exception)
        {
            return dataInFile;
        }

        return dataInFile;
    }


    public static bool WriteFile(string filePath, string content)
    {
        try
        {
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                using (var sw = new StreamWriter(stream))
                {
                    sw.Write(content);
                }
            }
        }
        catch (IOException)
        {
            return false;
        }
        catch (Exception)
        {
            return false;
        }
        return true;
    }



    public static bool FileExist(string filePath)
    {
        return File.Exists(filePath);
    }
}
