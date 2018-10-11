﻿using NUnit.Framework;

namespace RDotNet
{
    internal class CharacterVectorTest : RDotNetTestFixture
    {
        [Test]
        public void TestCharacter()
        {
            var engine = this.Engine;
            var vector = engine.Evaluate("x <- c('foo', NA, 'bar')").AsCharacter();
            Assert.That(vector.Length, Is.EqualTo(3));
            Assert.That(vector[0], Is.EqualTo("foo"));
            Assert.That(vector[1], Is.Null);
            Assert.That(vector[2], Is.EqualTo("bar"));
            vector[0] = null;
            Assert.That(vector[0], Is.Null);
            var logical = engine.Evaluate("is.na(x)").AsLogical();
            Assert.That(logical[0], Is.True);
            Assert.That(logical[1], Is.True);
            Assert.That(logical[2], Is.False);
        }

        [Test]
        public void TestUnicodeCharacter()
        {
            var engine = this.Engine;
            var vector = engine.Evaluate("x <- c('красавица Наталья', 'Un apôtre')").AsCharacter();
            var encoding = engine.Evaluate("Encoding(x)").AsCharacter();
            Assert.That(encoding[0], Is.EqualTo("UTF-8"));
            Assert.That(encoding[1], Is.EqualTo("UTF-8"));

            var chinesetCharacter = engine.CreateCharacter("中言语");
            var chineseTest = engine.Evaluate("x <- c('中言语', 'abc')").AsCharacter();

            var c = chinesetCharacter[0];

            Assert.That(vector.Length, Is.EqualTo(2));
            Assert.That(vector[0], Is.EqualTo("красавица Наталья"));
            Assert.That(vector[1], Is.EqualTo("Un apôtre"));
        }

        [Test]
        public void TestDotnetToR()
        {
            var engine = this.Engine;
            var vector = engine.Evaluate("x <- character(100)").AsCharacter();
            Assert.That(vector.Length, Is.EqualTo(100));
            Assert.That(vector[0], Is.EqualTo(""));
            vector[1] = "foo";
            vector[2] = "bar";
            var second = engine.Evaluate("x[2]").AsCharacter().ToArray();
            Assert.AreEqual(1, second.Length);
            Assert.AreEqual("foo", second[0]);

            var third = engine.Evaluate("x[3]").AsCharacter().ToArray();
            Assert.AreEqual(1, third.Length);
            Assert.AreEqual("bar", third[0]);
        }
    }
}