using UnityEngine;
using System.Collections;
using System.IO;
using System.Collections.Generic;

public static class TextHandler 
{
    private static string[] textToWrite;

    public static string mapName;
    public static string algorithmUsed;

    public static long initialPathCalcTime;
    public static List<long> subsequentPathCalcTimes;
    public static long gameCompleteTime;
    public static int seekerPathLength;

    private static long averageSubsequentTime;
    private static long lowestCalculationTime;
    private static long highestCalculationTime;

    public static void initializeNewValuesForHandler()
    {
        mapName = "";
        algorithmUsed = "";
        initialPathCalcTime = 0;
        subsequentPathCalcTimes = new List<long>();
        averageSubsequentTime = 0;
        gameCompleteTime = 0;
        seekerPathLength = 0;
    }

    public static void CreateText()
    {
        SubsequentTimeCalculations();
        string text = algorithmUsed + " " + mapName
            + " || Initial Time : " + initialPathCalcTime
            + " || Average Subsequent Time : " + averageSubsequentTime
            + " || Lowest Time : " + lowestCalculationTime
            + " || Highest Time : " + highestCalculationTime
            + " || Path Length : " + seekerPathLength
            + " || Game Complete Time : " + gameCompleteTime;
        textToWrite = new string[] { text };
    }

    private static void SubsequentTimeCalculations()
    {
        averageSubsequentTime  = initialPathCalcTime;
        lowestCalculationTime  = initialPathCalcTime;
        highestCalculationTime = initialPathCalcTime;
        for(int i = 0; i < subsequentPathCalcTimes.Count; i++)
        {
            averageSubsequentTime += subsequentPathCalcTimes[i];
            if (subsequentPathCalcTimes[i] > highestCalculationTime)
                highestCalculationTime = subsequentPathCalcTimes[i];
            if (subsequentPathCalcTimes[i] < lowestCalculationTime)
                lowestCalculationTime = subsequentPathCalcTimes[i];
        }
        averageSubsequentTime = averageSubsequentTime / (subsequentPathCalcTimes.Count + 1); 
    }

    public static void WriteToTextFile(string path)
    {
        string[] readText = File.ReadAllLines(@path);
        string[] newWriteText = new string[readText.Length + textToWrite.Length];
        readText.CopyTo(newWriteText, 0);
        textToWrite.CopyTo(newWriteText, readText.Length);
        File.WriteAllLines(@path, newWriteText);
    }
    public static void WriteToTextFile(string path, string[] lines)
    {
        string[] readText = File.ReadAllLines(@path);
        string[] newWriteText = new string[readText.Length + lines.Length];
        readText.CopyTo(newWriteText, 0);
        lines.CopyTo(newWriteText, readText.Length);
        File.WriteAllLines(@path, newWriteText);
    }
}
