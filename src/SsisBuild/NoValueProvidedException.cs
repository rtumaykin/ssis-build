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

namespace SsisBuild
{
    /// <summary>
    /// An exception class which is raised when a command line argument does not have a corresponding value. 
    /// Derives from <see cref="CommandLineParsingException"/> class.
    /// Example: ssisbuild.exe -Configuration.
    /// Since the value for -Configuration argument is missing, this exception will be thrown.
    /// </summary>
    public class NoValueProvidedException : CommandLineParsingException
    {
        /// <summary>
        /// Constructor for <see cref="NoValueProvidedException"/> class.
        /// </summary>
        /// <param name="argumentName">Name of an argument which value is not provided.</param>
        public NoValueProvidedException(string argumentName) : base($"No value provided for an argument {argumentName}.")
        {
        }
    }
}
