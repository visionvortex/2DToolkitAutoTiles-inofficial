using System;
using System.Collections.Generic;

namespace Extensions
{

  public static class ListExtensions
  {

    /// <summary>
    /// Shuffle any (I)List with an extension method based on the Fisher-Yates shuffle.<para/>
    /// Using Unityengine.Random.Range for seeding integration.<para/>
    /// http://stackoverflow.com/questions/273313/randomize-a-listt-in-c-sharp
    /// </summary>
    public static void Shuffle<T>(this IList<T> list) {
      //Random rng = new Random();
      int n = list.Count;
      while (n > 1) {
        n--;
        int k = UnityEngine.Random.Range(0, n + 1);
        //int k = rng.Next(n + 1);
        T value = list[k];
        list[k] = list[n];
        list[n] = value;
      }

    }


    /// <summary>
    /// This method returns a random member from the list.<para/>
    /// Using Unityengine.Random.Range for seeding integration.
    /// </summary>
    public static T GetRandom<T>(this IList<T> list) {
      if (list.Count == 0) {
        return default(T);
      }

      return list[UnityEngine.Random.Range(0, list.Count)];
    }

  }

}
