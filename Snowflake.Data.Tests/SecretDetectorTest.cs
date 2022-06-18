/*
 * Copyright (c) 2021 Snowflake Computing Inc. All rights reserved.
 */

using NUnit.Framework;
using Tortuga.Data.Snowflake.Tests.Mock;

namespace Tortuga.Data.Snowflake.Tests;

[TestFixture]
class SecretDetectorTest : SFBaseTest
{
	SecretDetector.Mask mask;

	[SetUp]
	public void BeforeTest()
	{
		mask = SecretDetector.MaskSecrets(null);
	}

	public void BasicMasking(string text)
	{
		mask = SecretDetector.MaskSecrets(text);
		Assert.IsFalse(mask.IsMasked);
		Assert.AreEqual(text, mask.MaskedText);
		Assert.IsNull(mask.ErrStr);
	}

	[Test]
	public void TestNullString()
	{
		BasicMasking(null);
	}

	[Test]
	public void TestEmptyString()
	{
		BasicMasking("");
	}

	[Test]
	public void TestNoMasking()
	{
		BasicMasking("This string is innocuous");
	}

	[Test]
	public void TestExceptionInMasking()
	{
		mask = MockSecretDetector.MaskSecrets("This string will raise an exception");
		Assert.IsTrue(mask.IsMasked);
		Assert.AreEqual("Test exception", mask.MaskedText);
		Assert.AreEqual("Test exception", mask.ErrStr);
	}

	public void BasicMasking(string text, string expectedText)
	{
		mask = SecretDetector.MaskSecrets(text);
		Assert.IsTrue(mask.IsMasked);
		Assert.AreEqual(expectedText, mask.MaskedText);
		Assert.IsNull(mask.ErrStr);
	}

	[Test]
	public void TestAWSKeys()
	{
		// aws_key_id
		BasicMasking(@"aws_key_id='aaaaaaaa'", @"aws_key_id='****'");

		// aws_secret_key
		BasicMasking(@"aws_secret_key='aaaaaaaa'", @"aws_secret_key='****'");

		// access_key_id
		BasicMasking(@"access_key_id='aaaaaaaa'", @"access_key_id='****'");

		// secret_access_key
		BasicMasking(@"secret_access_key='aaaaaaaa'", @"secret_access_key='****'");

		// aws_key_id with colon
		BasicMasking(@"aws_key_id:'aaaaaaaa'", @"aws_key_id:'****'");

		// aws_key_id with single quote on key
		BasicMasking(@"'aws_key_id':'aaaaaaaa'", @"'aws_key_id':'****'");

		// aws_key_id with double quotes on key
		BasicMasking(@"""aws_key_id"":'aaaaaaaa'", @"""aws_key_id"":'****'");

		//If attribute is enclose in single or double quote
		BasicMasking(@"'aws_key_id'='aaaaaaaa'", @"'aws_key_id'='****'");
		BasicMasking(@"""aws_key_id""='aaaaaaaa'", @"""aws_key_id""='****'");

		//aws_key_id|aws_secret_key|access_key_id|secret_access_key)('|"")?(\s*[:|=]\s*)'([^']+)'
		// Delimiters before start of value to mask
		BasicMasking(@"aws_key_id:'aaaaaaaa'", @"aws_key_id:'****'");
		BasicMasking(@"aws_key_id='aaaaaaaa'", @"aws_key_id='****'");
	}

	[Test]
	public void TestAWSTokens()
	{
		// accessToken
		BasicMasking(@"accessToken"":""aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa""", @"accessToken"":""XXXX""");

		// tempToken
		BasicMasking(@"tempToken"":""aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa""", @"tempToken"":""XXXX""");

		// keySecret
		BasicMasking(@"keySecret"":""aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa""", @"keySecret"":""XXXX""");

		// Verify that all allowed characters are correctly supported
		BasicMasking(@"accessToken""  :  ""aB1aaaaaaaaaaZaaaaaaaaaaa9aaaaaaa=""", @"accessToken"":""XXXX""");
		BasicMasking(@"accessToken""  :  ""aB1aaaaaaaaaaZaaaaaaaa56aaaaaaaaaa==""", @"accessToken"":""XXXX""");
	}

	[Test]
	public void TestAWSServerSide()
	{
		// amz encryption
		BasicMasking(@"x-amz-server-side-encryption-customer-key:YtLf9S7iLprBMxSpP0Scm5MNgtsmK12hNd63wRpOGfI=",
			@"x-amz-server-side-encryption-customer-key:....");

		// amz encryption md5
		BasicMasking(@"x-amz-server-side-encryption-customer-key-md5:5SBvdH9fHaWsORVu7auB/A==",
			@"x-amz-server-side-encryption-customer-key-md5:....");

		// amz encryption algorithm
		BasicMasking(@"x-amz-server-side-encryption-customer-algorithm: ABC123",
			@"x-amz-server-side-encryption-customer-algorithm:....");

		// Verify that all allowed characters are correctly supported
		BasicMasking(@"x-amz-server-side-encryptionthis-and-that: Scm5M=d/6_p-r5+/:j=8",
			@"x-amz-server-side-encryptionthis-and-that:....");
	}

	[Test]
	public void TestSASTokens()
	{
		// sig
		BasicMasking(@"sig=aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", @"sig=****");

		// signature
		BasicMasking(@"signature=aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", @"signature=****");

		// AWSAccessKeyId
		BasicMasking(@"AWSAccessKeyId=ABCDEFGHIJKL01234", @"AWSAccessKeyId=****");

		// password
		BasicMasking(@"password=aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", @"password=****");

		// passcode
		BasicMasking(@"passcode=aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", @"passcode=****");

		// Verify that all allowed characters are correctly supported
		BasicMasking(@"sig=abCaa09aaa%%aaaaaaaaaa/aaaaa+aaaaa", @"sig=****");
	}

	[Test]
	public void TestPrivateKey()
	{
		// Verify that all allowed characters are correctly supported
		BasicMasking("-----BEGIN PRIVATE KEY-----\na0a==aaB/aa1aaaa\naaaaCaaa+aa95aaa\n-----END PRIVATE KEY-----",
			"-----BEGIN PRIVATE KEY-----\\\\nXXXX\\\\n-----END PRIVATE KEY-----");
	}

	[Test]
	public void TestPrivateKeyData()
	{
		BasicMasking(@"""privateKeyData"": ""aaaaaaaaaa""", @"""privateKeyData"": ""XXXX""");

		// Verify that all allowed characters are correctly supported
		BasicMasking(@"""privateKeyData"": ""a/b+c=d0" + "\n" + "139\"", @"""privateKeyData"": ""XXXX""");
	}

	[Test]
	public void TestConnectionTokens()
	{
		// token
		BasicMasking(@"token:aaaaaaaa", @"token:****");

		// assertion content
		BasicMasking(@"assertion content:aaaaaaaa", @"assertion content:****");

		// Delimiters before start of value to mask
		BasicMasking(@"token""aaaaaaaa", @"token""****"); // "
		BasicMasking(@"token'aaaaaaaa", @"token'****"); // '
		BasicMasking(@"token=aaaaaaaa", @"token=****"); // =
		BasicMasking(@"token aaaaaaaa", @"token ****"); // {space}
		BasicMasking(@"token ="" 'aaaaaaaa", @"token ="" '****"); // Mix

		// Verify that all allowed characters are correctly supported
		BasicMasking(@"Token:a=b/c_d-e+F:025", @"Token:****");
	}

	[Test]
	public void TestPassword()
	{
		// password
		BasicMasking(@"password:aaaaaaaa", @"password:****");

		// pwd
		BasicMasking(@"pwd:aaaaaaaa", @"pwd:****");

		// passcode
		BasicMasking(@"passcode:aaaaaaaa", @"passcode:****");

		// Delimiters before start of value to mask
		BasicMasking(@"password""aaaaaaaa", @"password""****"); // "
		BasicMasking(@"password'aaaaaaaa", @"password'****"); // '
		BasicMasking(@"password=aaaaaaaa", @"password=****"); // =
		BasicMasking(@"password aaaaaaaa", @"password ****"); // {space}
		BasicMasking(@"password ="" 'aaaaaaaa", @"password ="" '****"); // Mix

		// Verify that all allowed characters are correctly supported
		BasicMasking(@"password:a!b""c#d$e%f&g'h(i)k*k+l,m;n<o=p>q?r@s[t]u^v_w`x{y|z}Az0123", @"password:****");
	}

	[Test]
	public void TestMaskToken()
	{
		var longToken = "_Y1ZNETTn5/qfUWj3Jedby7gipDzQs=U" +
			 "KyJH9DS=nFzzWnfZKGV+C7GopWCGD4Lj" +
			 "OLLFZKOE26LXHDt3pTi4iI1qwKuSpf/F" +
			 "mClCMBSissVsU3Ei590FP0lPQQhcSGcD" +
			 "u69ZL_1X6e9h5z62t/iY7ZkII28n2qU=" +
			 "nrBJUgPRCIbtJQkVJXIuOHjX4G5yUEKj" +
			 "ZBAx4w6=_lqtt67bIA=o7D=oUSjfywsR" +
			 "FoloNIkBPXCwFTv+1RVUHgVA2g8A9Lw5" +
			 "XdJYuI8vhg=f0bKSq7AhQ2Bh";

		var tokenStrWithPrefix = "Token =" + longToken;
		mask = SecretDetector.MaskSecrets(tokenStrWithPrefix);
		Assert.IsTrue(mask.IsMasked);
		Assert.AreEqual(@"Token =****", mask.MaskedText);
		Assert.IsNull(mask.ErrStr);

		var idTokenStrWithPrefix = "idToken : " + longToken;
		mask = SecretDetector.MaskSecrets(idTokenStrWithPrefix);
		Assert.IsTrue(mask.IsMasked);
		Assert.AreEqual(@"idToken : ****", mask.MaskedText);
		Assert.IsNull(mask.ErrStr);

		var sessionTokenStrWithPrefix = "sessionToken : " + longToken;
		mask = SecretDetector.MaskSecrets(sessionTokenStrWithPrefix);
		Assert.IsTrue(mask.IsMasked);
		Assert.AreEqual(@"sessionToken : ****", mask.MaskedText);
		Assert.IsNull(mask.ErrStr);

		var masterTokenStrWithPrefix = "masterToken : " + longToken;
		mask = SecretDetector.MaskSecrets(masterTokenStrWithPrefix);
		Assert.IsTrue(mask.IsMasked);
		Assert.AreEqual(@"masterToken : ****", mask.MaskedText);
		Assert.IsNull(mask.ErrStr);

		var assertionStrWithPrefix = "assertion content: " + longToken;
		mask = SecretDetector.MaskSecrets(assertionStrWithPrefix);
		Assert.IsTrue(mask.IsMasked);
		Assert.AreEqual(@"assertion content: ****", mask.MaskedText);
		Assert.IsNull(mask.ErrStr);

		var snowFlakeAuthToken = "Authorization: Snowflake Token=\"ver:1-hint:92019676298218-ETMsDgAAAXswwgJhABRBRVMvQ0JDL1BLQ1M1UGFkZGluZwEAABAAEF1tbNM3myWX6A9sNSK6rpIAAACA6StojDJS4q1Vi3ID+dtFEucCEvGMOte0eapK+reb39O6hTHYxLfOgSGsbvbM5grJ4dYdNJjrzDf1r07tID4I2RJJRYjS4/DWBJn98Untd3xeNnXE1/45HgvwKVHlmZQLVwfWAxI7ifl2MVDwJlcXBufLZoVMYhUd4np121d7zFwAFGQzKyzUYQwI3M9Nqja9syHgaotG\"";
		mask = SecretDetector.MaskSecrets(snowFlakeAuthToken);
		Assert.IsTrue(mask.IsMasked);
		Assert.AreEqual(@"Authorization: Snowflake Token=""****""", mask.MaskedText);
		Assert.IsNull(mask.ErrStr);
	}

	[Test]
	public void TestTokenFalsePositive()
	{
		var falsePositiveToken = "2020-04-30 23:06:04,069 - MainThread auth.py:397" +
			" - write_temporary_credential() - DEBUG - no ID " +
			"token is given when try to store temporary credential";

		mask = SecretDetector.MaskSecrets(falsePositiveToken);
		Assert.IsFalse(mask.IsMasked);
		Assert.AreEqual(falsePositiveToken, mask.MaskedText);
		Assert.IsNull(mask.ErrStr);
	}

	[Test]
	public void TestPasswords()
	{
		var randomPassword = "Fh[+2J~AcqeqW%?";

		var randomPasswordWithPrefix = "password:" + randomPassword;
		mask = SecretDetector.MaskSecrets(randomPasswordWithPrefix);
		Assert.IsTrue(mask.IsMasked);
		Assert.AreEqual(@"password:****", mask.MaskedText);
		Assert.IsNull(mask.ErrStr);

		var randomPasswordCaps = "PASSWORD:" + randomPassword;
		mask = SecretDetector.MaskSecrets(randomPasswordCaps);
		Assert.IsTrue(mask.IsMasked);
		Assert.AreEqual(@"PASSWORD:****", mask.MaskedText);
		Assert.IsNull(mask.ErrStr);

		var randomPasswordMixCase = "PassWorD:" + randomPassword;
		mask = SecretDetector.MaskSecrets(randomPasswordMixCase);
		Assert.IsTrue(mask.IsMasked);
		Assert.AreEqual(@"PassWorD:****", mask.MaskedText);
		Assert.IsNull(mask.ErrStr);

		var randomPasswordEqualSign = "password = " + randomPassword;
		mask = SecretDetector.MaskSecrets(randomPasswordEqualSign);
		Assert.IsTrue(mask.IsMasked);
		Assert.AreEqual(@"password = ****", mask.MaskedText);
		Assert.IsNull(mask.ErrStr);

		var randomPwdWithPrefix = "pwd:" + randomPassword;
		mask = SecretDetector.MaskSecrets(randomPwdWithPrefix);
		Assert.IsTrue(mask.IsMasked);
		Assert.AreEqual(@"pwd:****", mask.MaskedText);
		Assert.IsNull(mask.ErrStr);
	}

	[Test]
	public void TestTokenPassword()
	{
		var longToken = "_Y1ZNETTn5/qfUWj3Jedby7gipDzQs=U" +
			 "KyJH9DS=nFzzWnfZKGV+C7GopWCGD4Lj" +
			 "OLLFZKOE26LXHDt3pTi4iI1qwKuSpf/F" +
			 "mClCMBSissVsU3Ei590FP0lPQQhcSGcD" +
			 "u69ZL_1X6e9h5z62t/iY7ZkII28n2qU=" +
			 "nrBJUgPRCIbtJQkVJXIuOHjX4G5yUEKj" +
			 "ZBAx4w6=_lqtt67bIA=o7D=oUSjfywsR" +
			 "FoloNIkBPXCwFTv+1RVUHgVA2g8A9Lw5" +
			 "XdJYuI8vhg=f0bKSq7AhQ2Bh";

		var longToken2 = "ktL57KJemuq4-M+Q0pdRjCIMcf1mzcr" +
			  "MwKteDS5DRE/Pb+5MzvWjDH7LFPV5b_" +
			  "/tX/yoLG3b4TuC6Q5qNzsARPPn_zs/j" +
			  "BbDOEg1-IfPpdsbwX6ETeEnhxkHIL4H" +
			  "sP-V";

		var randomPwd = "Fh[+2J~AcqeqW%?";
		var randomPwd2 = randomPwd + "vdkav13";

		var testStringWithPrefix = $"token={longToken} random giberish password:{randomPwd}";
		mask = SecretDetector.MaskSecrets(testStringWithPrefix);
		Assert.IsTrue(mask.IsMasked);
		Assert.AreEqual("token=**** random giberish password:****", mask.MaskedText);
		Assert.IsNull(mask.ErrStr);

		// order reversed
		testStringWithPrefix = $"password:{randomPwd} random giberish token={longToken}";
		mask = SecretDetector.MaskSecrets(testStringWithPrefix);
		Assert.IsTrue(mask.IsMasked);
		Assert.AreEqual("password:**** random giberish token=****", mask.MaskedText);
		Assert.IsNull(mask.ErrStr);

		// multiple tokens and password
		testStringWithPrefix = $"token={longToken} random giberish password:{randomPwd} random giberish idToken:{longToken2}";
		mask = SecretDetector.MaskSecrets(testStringWithPrefix);
		Assert.IsTrue(mask.IsMasked);
		Assert.AreEqual("token=**** random giberish password:**** random giberish idToken:****", mask.MaskedText);
		Assert.IsNull(mask.ErrStr);

		// two passwords
		testStringWithPrefix = $"password={randomPwd} random giberish pwd:{randomPwd2}";
		mask = SecretDetector.MaskSecrets(testStringWithPrefix);
		Assert.IsTrue(mask.IsMasked);
		Assert.AreEqual("password=**** random giberish pwd:****", mask.MaskedText);
		Assert.IsNull(mask.ErrStr);

		// multiple passwords
		testStringWithPrefix = $"password={randomPwd} random giberish password={randomPwd2} random giberish password={randomPwd}";
		mask = SecretDetector.MaskSecrets(testStringWithPrefix);
		Assert.IsTrue(mask.IsMasked);
		Assert.AreEqual("password=**** random giberish password=**** random giberish password=****", mask.MaskedText);
		Assert.IsNull(mask.ErrStr);
	}

	[Test]
	public void TestCustomPattern()
	{
		var regex = new string[2]
		{
			@"(testCustomPattern\s*:\s*""([a-z]{8,})"")",
			@"(testCustomPattern\s*:\s*""([0-9]{8,})"")"
		};
		var masks = new string[2]
		{
			"maskCustomPattern1",
			"maskCustomPattern2"
		};

		SecretDetector.SetCustomPatterns(regex, masks);

		// Mask custom pattern
		var testString = "testCustomPattern: \"abcdefghijklmnop\"";
		mask = SecretDetector.MaskSecrets(testString);
		Assert.IsTrue(mask.IsMasked);
		Assert.AreEqual(masks[0], mask.MaskedText);
		Assert.IsNull(mask.ErrStr);

		testString = "testCustomPattern: \"1234567890\"";
		mask = SecretDetector.MaskSecrets(testString);
		Assert.IsTrue(mask.IsMasked);
		Assert.AreEqual(masks[1], mask.MaskedText);
		Assert.IsNull(mask.ErrStr);

		// Mask password and custom pattern
		testString = "password: abcdefghijklmnop testCustomPattern: \"abcdefghijklmnop\"";
		mask = SecretDetector.MaskSecrets(testString);
		Assert.IsTrue(mask.IsMasked);
		Assert.AreEqual("password: **** " + masks[0], mask.MaskedText);
		Assert.IsNull(mask.ErrStr);

		testString = "password: abcdefghijklmnop testCustomPattern: \"1234567890\"";
		mask = SecretDetector.MaskSecrets(testString);
		Assert.IsTrue(mask.IsMasked);
		Assert.AreEqual("password: **** " + masks[1], mask.MaskedText);
		Assert.IsNull(mask.ErrStr);
	}

	[Test]
	public void TestCustomPatternClear()
	{
		var regex = new string[1] { @"(testCustomPattern\s*:\s*""([a-z]{8,})"")" };
		var masks = new string[1] { "maskCustomPattern1" };

		SecretDetector.SetCustomPatterns(regex, masks);

		// Mask custom pattern
		var testString = "testCustomPattern: \"abcdefghijklmnop\"";
		mask = SecretDetector.MaskSecrets(testString);
		Assert.IsTrue(mask.IsMasked);
		Assert.AreEqual(masks[0], mask.MaskedText);
		Assert.IsNull(mask.ErrStr);

		// Clear custom patterns
		SecretDetector.ClearCustomPatterns();
		testString = "testCustomPattern: \"abcdefghijklmnop\"";
		mask = SecretDetector.MaskSecrets(testString);
		Assert.IsFalse(mask.IsMasked);
		Assert.AreEqual(testString, mask.MaskedText);
		Assert.IsNull(mask.ErrStr);
	}

	[Test]
	public void TestCustomPatternUnequalCount()
	{
		var regex = Array.Empty<string>();
		var masks = new string[1] { "maskCustomPattern1" };

		// Masks count is greater than regex
		try
		{
			SecretDetector.SetCustomPatterns(regex, masks);
		}
		catch (Exception ex)
		{
			Assert.AreEqual("Regex count and mask count must be equal.", ex.Message);
		}

		// Regex count is greater than masks
		regex = new string[2]
		{
			@"(testCustomPattern\s*:\s*""([0-9]{8,})"")",
			@"(testCustomPattern\s*:\s*""([0-9]{8,})"")"
		};
		try
		{
			SecretDetector.SetCustomPatterns(regex, masks);
		}
		catch (Exception ex)
		{
			Assert.AreEqual("Regex count and mask count must be equal.", ex.Message);
		}
	}

	[Test]
	public void TestHttpResponse()
	{
		string randomHttpResponse =
			"\"data\" : {" +
			"\"masterToken\" : \"ver:1-hint:92019676298218-ETMsDgAAAXrK7h+Y=" +
			"\"token\" : \"_Y1ZNETTn5/qfUWj3Jedby7gipDzQs=U" +
			"\"remMeValidityInSeconds\" : 0," +
			"\"healthCheckInterval\" : 12," +
			"\"newClientForUpgrade\" : null," +
			"\"sessionId\" : 1234";

		var randomHttpResponseWithPrefix = "Post response: " + randomHttpResponse;
		mask = SecretDetector.MaskSecrets(randomHttpResponseWithPrefix);
		Assert.IsTrue(mask.IsMasked);
		Assert.AreEqual(
			"Post response: " +
			"\"data\" : {" +
			"\"masterToken\" : \"****" +
			"\"token\" : \"****" +
			"\"remMeValidityInSeconds\" : 0," +
			"\"healthCheckInterval\" : 12," +
			"\"newClientForUpgrade\" : null," +
			"\"sessionId\" : 1234",
			mask.MaskedText);
		Assert.IsNull(mask.ErrStr);
	}
}
