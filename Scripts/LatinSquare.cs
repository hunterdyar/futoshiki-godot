using System;
using System.Collections.Generic;
using Matrix = System.Collections.Generic.List<System.Collections.Generic.List<int>>;

public static class LatinSquare
{
    private static readonly Random rng = new Random();

    public static void Shuffle<T>(this IList<T> list) {
        int n = list.Count;
        while (n > 1) {
            n--;
            int k = rng.Next(n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }
    
   public static int[,] CreateLatinSquare(int n) {
        if (n <= 0) {
            Console.WriteLine("[]");
            return null;
        }

        var latin = new Matrix();
        for (int i = 0; i < n; i++) {
            List<int> temp = new List<int>();
            for (int j = 0; j < n; j++) {
                temp.Add(j);
            }
            latin.Add(temp);
        }
        // first row
        latin[0].Shuffle();

        // middle row(s)
        for (int i = 1; i < n - 1; i++) {
            bool shuffled = false;

            while (!shuffled) {
                latin[i].Shuffle();
                for (int k = 0; k < i; k++) {
                    for (int j = 0; j < n; j++) {
                        if (latin[k][j] == latin[i][j]) {
                            goto shuffling;
                        }
                    }
                }
                shuffled = true;

            shuffling: { }
            }
        }

        // last row
        for (int j = 0; j < n; j++) {
            List<bool> used = new List<bool>();
            for (int i = 0; i < n; i++) {
                used.Add(false);
            }

            for (int i = 0; i < n-1; i++) {
                used[latin[i][j]] = true;
            }
            for (int k = 0; k < n; k++) {
                if (!used[k]) {
                    latin[n - 1][j] = k;
                    break;
                }
            }
        }

        var final =new int[n, n];
        //increment all by 1
        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < n; j++)
            {
                final[i,j] = latin[i][j]+1;
            }
        }

        
        return final;
    }
}
