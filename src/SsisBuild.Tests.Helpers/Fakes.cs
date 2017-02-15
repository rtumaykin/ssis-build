//-----------------------------------------------------------------------
//   Copyright 2017 Roman Tumaykin
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//       http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.
//-----------------------------------------------------------------------

using System;
using System.Linq;

namespace SsisBuild.Tests.Helpers
{
    public class Fakes
    {
        private static readonly Random Random = new Random();
        public static string RandomString(int minLength = 10, int maxLength = 100)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, Random.Next(minLength, maxLength))
              .Select(s => s[Random.Next(s.Length)]).ToArray());
        }

        public static T RandomEnum<T>()
        {
            var v = Enum.GetValues(typeof(T));
            return (T)v.GetValue(Random.Next(v.Length));
        }

        public static bool RandomBool()
        {
            return Random.NextDouble() < 0.5;
        }

        public static int RandomInt(int min = int.MinValue, int max = int.MaxValue)
        {
            return Random.Next(min, max);
        }
    }
}
