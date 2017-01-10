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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace SsisBuild
{
    public enum ReleaseNotesType
    {
        Simple,
        Complex,
        Invalid
    }

    public static class ReleaseNotesHelper
    {
        public static ReleaseNotes ParseReleaseNotes(string releaseNotesFilePath)
        {
            var releaseNoteLines = ReadLatestReleaseNotes(releaseNotesFilePath);
            var noteLines = releaseNoteLines as string[] ?? releaseNoteLines.ToArray();
            var notesType = GetReleaseNotesType(noteLines[0]);

            if (noteLines.Length == 0 || notesType == ReleaseNotesType.Invalid)
                return null;

            if (notesType == ReleaseNotesType.Simple)
                return ProcessSimpleNotes(noteLines[0]);

            return ProcessComplexNotes(noteLines);
        }

        private static ReleaseNotes ProcessComplexNotes(string[] noteLines)
        {
            return new ReleaseNotes()
            {
                Version = ParseVersion(noteLines[0]),
                Notes = noteLines.Where(nl=>!nl.StartsWith("##")).Select(nl=>nl.Trim(' ', '*')).ToList()
            };
        }

        private static ReleaseNotes ProcessSimpleNotes(string noteLine)
        {
            var notesLineSplit = noteLine.Split(new[] {'-'}, 2);


            return new ReleaseNotes()
            {
                Version = ParseVersion(noteLine),
                Notes = new List<string>(notesLineSplit.Length > 1 ? new[] {notesLineSplit[1].Trim()} : new string[] {})
            };
        }


        public static ReleaseNotesType GetReleaseNotesType(string firstLine)
        {
            if (firstLine.StartsWith("*"))
                return ReleaseNotesType.Simple;

            if (firstLine.StartsWith("##"))
                return ReleaseNotesType.Complex;

            return ReleaseNotesType.Invalid;
        }


        private static IEnumerable<string> ReadLatestReleaseNotes(string releaseNotesFilePath)
        {
            var notes = File.ReadAllLines(releaseNotesFilePath);

            if (notes.Length == 0)
                yield break;

            var notesType = GetReleaseNotesType(notes[0]);

            switch (notesType)
            {
                case ReleaseNotesType.Invalid:
                    yield break;
                case ReleaseNotesType.Simple:
                    yield return notes[0];
                    break;
            }

            foreach (var note in notes)
            {
                if (string.IsNullOrWhiteSpace(note))
                    break;

                yield return note;
            }
        }

        private static Version ParseVersion(string input)
        {
            var regex = new Regex(@"[0-9]+\.[0-9]+\.[0-9]+");

            var match = regex.Match(input);
            if (match.Success)
                return Version.Parse(match.Value);

            throw new Exception($"Unable to parse version from the string: {input}.");
        }
    }


    public class ReleaseNotes
    {
        public Version Version { get; set; }
        public List<string> Notes { get; set; }
    }
}
