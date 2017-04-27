using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using System.IO;

namespace Core.TransactionServer.UnitTest
{
    [TestFixture]
    internal class CacheFileTest
    {
        [Test]
        public void ParseSequenceTest()
        {
            string fileName = "aaadc4a8-7ee1-4d62-a355-7a36518a2099_1.tcf";
            string fileSuffix = "tcf";
        }

        [Test]
        public void GetFileExtensionTest()
        {
            string filePath = @"d:\2d6d87b2-d4f4-48b7-a429-0cce782f3ebb_1.rsf";
            string extension = Path.GetExtension(filePath);
            Assert.AreEqual("rsf", extension);
        }

    }
}
