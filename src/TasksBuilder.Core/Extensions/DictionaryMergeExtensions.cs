﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SInnovations.VSTeamServices.TasksBuilder.Extensions
{
    public static class DictionaryMergeExtensions
    {
        public static string CalculateMD5Hash(string input)
        {
            // step 1, calculate MD5 hash from input
            MD5 md5 = System.Security.Cryptography.MD5.Create();
            byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
            byte[] hash = md5.ComputeHash(inputBytes);

            // step 2, convert byte array to hex string
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("X2"));
            }

            return sb.ToString();
        }
        public static bool MergeChanged(this IDictionary<string, string> dicts, params IDictionary<string, string>[] others)
        {
            return dicts.MergeChangedImp(true, others);
        }
        public static bool MergeChangedReversed(this IDictionary<string, string> dicts, params IDictionary<string, string>[] others)
        {
            return dicts.MergeChangedImp(false, others);
        }
        private static bool MergeChangedImp(this IDictionary<string, string> dicts, bool first, params IDictionary<string, string>[] others)
        {
            var newDict = new[] { dicts }.Concat(others).SelectMany(dict => dict)
                   .ToLookup(pair => pair.Key, pair => pair.Value)
                   .ToDictionary(group => group.Key, group => first ? group.First() : group.Last());
            var old = CalculateMD5Hash(string.Join("", dicts.Keys.OrderBy(s => s).Select(s => dicts[s])));
            var newhash = CalculateMD5Hash(string.Join("", newDict.Keys.OrderBy(s => s).Select(s => newDict[s])));
            if (old == newhash)
                return false;
            dicts.Clear();
            foreach (var d in newDict)
                dicts.Add(d);
            return true;

        }
    }
}
