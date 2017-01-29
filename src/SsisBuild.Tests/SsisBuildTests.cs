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
using Moq;
using Xunit;

namespace SsisBuild.Tests
{
    public class SsisBuildTests : IDisposable
    {
        private readonly Mock<IBuildArguments> _buildArgumentsMock;
        private readonly Mock<IBuilder> _builderMock;

        public SsisBuildTests()
        {
            _buildArgumentsMock =  new Mock<IBuildArguments>();
            _builderMock = new Mock<IBuilder>();
        }

        [Fact]
        public void Fail_Main_ArgumentProcessingException()
        {
            var buildArguments = _buildArgumentsMock.Object;
            var testException = new InvalidArgumentException("SomeProp", "SomeValue");

            _buildArgumentsMock.Setup(b => b.ProcessArgs(It.IsAny<string[]>())).Throws(testException);

            var exception = Record.Exception(() => Program.MainInternal(new[] {"really anything"}, _builderMock.Object, buildArguments));

            Assert.IsType<InvalidArgumentException>(exception);
            Assert.Equal(exception.Message, testException.Message, StringComparer.InvariantCultureIgnoreCase);
        }

        [Fact]
        public void Fail_Main_BuilderException()
        {
            var buildArguments = _buildArgumentsMock.Object;
            var testException = new Exception("Some Message");
            _builderMock.Setup(b => b.Execute(buildArguments)).Throws(testException);

            var exception = Record.Exception(() => Program.MainInternal(new[] { "really anything" }, _builderMock.Object, buildArguments));

            Assert.IsType<Exception>(exception);
            Assert.Equal(exception.Message, testException.Message, StringComparer.InvariantCultureIgnoreCase);
        }


        [Fact]
        public void Pass_Main_NoException()
        {
            var buildArguments = _buildArgumentsMock.Object;

            var exception = Record.Exception(() => Program.MainInternal(new[] { "really anything" }, _builderMock.Object, buildArguments));

            Assert.Null(exception);
        }

        public void Dispose()
        {

        }
    }
}
