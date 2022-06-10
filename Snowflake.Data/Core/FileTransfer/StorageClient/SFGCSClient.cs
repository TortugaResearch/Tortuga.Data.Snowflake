/*
 * Copyright (c) 2021 Snowflake Computing Inc. All rights reserved.
 */

#nullable enable

using Google.Apis.Auth.OAuth2;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using Tortuga.HttpClientUtilities;

namespace Tortuga.Data.Snowflake.Core.FileTransfer.StorageClient;

/// <summary>
/// The GCS client used to transfer files to the remote Google Cloud Storage.
/// </summary>
class SFGCSClient : ISFRemoteStorageClient
{
	/// <summary>
	/// GCS header values.
	/// </summary>
	const string GCS_METADATA_PREFIX = "x-goog-meta-";

	const string GCS_METADATA_SFC_DIGEST = GCS_METADATA_PREFIX + "sfc-digest";
	const string GCS_METADATA_MATDESC_KEY = GCS_METADATA_PREFIX + "matdesc";
	const string GCS_METADATA_ENCRYPTIONDATAPROP = GCS_METADATA_PREFIX + "encryptiondata";
	const string GCS_FILE_HEADER_CONTENT_LENGTH = "x-goog-stored-content-length";

	/// <summary>
	/// The attribute in the credential map containing the access token.
	/// </summary>
	const string GCS_ACCESS_TOKEN = "GCS_ACCESS_TOKEN";

	/// <summary>
	/// The storage client.
	/// </summary>
	readonly Google.Cloud.Storage.V1.StorageClient m_StorageClient;

	/// <summary>
	/// The HTTP client to make requests.
	/// </summary>
	readonly HttpClient m_HttpClient;

	/// <summary>
	/// GCS client with access token.
	/// </summary>
	/// <param name="stageInfo">The command stage info.</param>
	public SFGCSClient(PutGetStageInfo stageInfo)
	{
		if (stageInfo.stageCredentials == null)
			throw new ArgumentException("stageInfo.stageCredentials is null", nameof(stageInfo));

		if (stageInfo.stageCredentials.TryGetValue(GCS_ACCESS_TOKEN, out string? accessToken))
		{
			var creds = GoogleCredential.FromAccessToken(accessToken, null);
			m_StorageClient = Google.Cloud.Storage.V1.StorageClient.Create(creds);
		}
		else
		{
			m_StorageClient = Google.Cloud.Storage.V1.StorageClient.CreateUnauthenticated();
		}

		m_HttpClient = new HttpClient();
	}

	RemoteLocation ISFRemoteStorageClient.ExtractBucketNameAndPath(string stageLocation) => ExtractBucketNameAndPath(stageLocation);

	/// <summary>
	/// Extract the bucket name and path from the stage location.
	/// </summary>
	/// <param name="stageLocation">The command stage location.</param>
	/// <returns>The remote location of the GCS file.</returns>
	static public RemoteLocation ExtractBucketNameAndPath(string stageLocation)
	{
		var containerName = stageLocation;
		var gcsPath = "";

		// Split stage location as bucket name and path
		if (stageLocation.Contains("/"))
		{
			containerName = stageLocation.Substring(0, stageLocation.IndexOf('/'));

			gcsPath = stageLocation.Substring(stageLocation.IndexOf('/') + 1,
				stageLocation.Length - stageLocation.IndexOf('/') - 1);
			if (gcsPath != null && !gcsPath.EndsWith("/"))
			{
				gcsPath += '/';
			}
		}

		return new RemoteLocation()
		{
			bucket = containerName,
			key = gcsPath
		};
	}

	/// <summary>
	/// Get the file header.
	/// </summary>
	/// <param name="fileMetadata">The GCS file metadata.</param>
	/// <returns>The file header of the GCS file.</returns>
	public FileHeader? GetFileHeader(SFFileMetadata fileMetadata)
	{
		// If file already exists, return
		if (fileMetadata.resultStatus == ResultStatus.UPLOADED.ToString() ||
			fileMetadata.resultStatus == ResultStatus.DOWNLOADED.ToString())
		{
			return new FileHeader
			{
				digest = fileMetadata.sha256Digest,
				contentLength = fileMetadata.srcFileSize,
				encryptionMetadata = fileMetadata.encryptionMetadata
			};
		}

		if (fileMetadata.presignedUrl != null)
		{
			// Issue GET request to GCS file URL
			try
			{
				var response = m_HttpClient.GetStream(fileMetadata.presignedUrl);
			}
			catch (HttpRequestException err)
			{
				if (err.Message.Contains("401") ||
					err.Message.Contains("403") ||
					err.Message.Contains("404"))
				{
					fileMetadata.resultStatus = ResultStatus.NOT_FOUND_FILE.ToString();
					return new FileHeader();
				}
			}
		}
		else
		{
			// Generate the file URL based on GCS location
			//var url = generateFileURL(fileMetadata.stageInfo.location, fileMetadata.destFileName);
			try
			{
				// Issue a GET response
				m_HttpClient.DefaultRequestHeaders.Add("Authorization", "Bearer ${accessToken}");
				var response = m_HttpClient.Get(fileMetadata.presignedUrl);

				var digest = response.Headers.GetValues(GCS_METADATA_SFC_DIGEST);
				var contentLength = response.Headers.GetValues("content-length");

				fileMetadata.resultStatus = ResultStatus.UPLOADED.ToString();

				return new FileHeader
				{
					digest = digest.ToString(),
					contentLength = Convert.ToInt64(contentLength)
				};
			}
			catch (HttpRequestException err)
			{
				// If file doesn't exist, GET request fails
				fileMetadata.lastError = err;
				if (err.Message.Contains("401"))
				{
					fileMetadata.resultStatus = ResultStatus.RENEW_TOKEN.ToString();
				}
				else if (err.Message.Contains("403") ||
					err.Message.Contains("500") ||
					err.Message.Contains("503"))
				{
					fileMetadata.resultStatus = ResultStatus.NEED_RETRY.ToString();
				}
				else if (err.Message.Contains("404"))
				{
					fileMetadata.resultStatus = ResultStatus.NOT_FOUND_FILE.ToString();
				}
				else
				{
					fileMetadata.resultStatus = ResultStatus.ERROR.ToString();
				}
			}
		}
		return null;
	}

	/// <summary>
	/// Generate the file URL.
	/// </summary>
	/// <param name="stageLocation">The GCS file metadata.</param>
	/// <param name="fileName">The GCS file metadata.</param>
	static string generateFileURL(string stageLocation, string fileName)
	{
		var gcsLocation = ExtractBucketNameAndPath(stageLocation);
		var fullFilePath = gcsLocation.key + fileName;
		var link = "https://storage.googleapis.com/" + gcsLocation.bucket + "/" + fullFilePath;
		return link;
	}

	/// <summary>
	/// Upload the file to the GCS location.
	/// </summary>
	/// <param name="fileMetadata">The GCS file metadata.</param>
	/// <param name="fileBytes">The file bytes to upload.</param>
	/// <param name="encryptionMetadata">The encryption metadata for the header.</param>
	public void UploadFile(SFFileMetadata fileMetadata, byte[] fileBytes, SFEncryptionMetadata encryptionMetadata)
	{
		// Create the encryption header value
		var encryptionData = JsonConvert.SerializeObject(new EncryptionData
		{
			EncryptionMode = "FullBlob",
			WrappedContentKey = new WrappedContentInfo
			{
				KeyId = "symmKey1",
				EncryptedKey = encryptionMetadata.key,
				Algorithm = "AES_CBC_256"
			},
			EncryptionAgent = new EncryptionAgentInfo
			{
				Protocol = "1.0",
				EncryptionAlgorithm = "AES_CBC_256"
			},
			ContentEncryptionIV = encryptionMetadata.iv,
			KeyWrappingMetadata = new KeyWrappingMetadataInfo
			{
				EncryptionLibrary = "Java 5.3.0"
			}
		});

		// Set the meta header values
		m_HttpClient.DefaultRequestHeaders.Add("x-goog-meta-sfc-digest", fileMetadata.sha256Digest);
		m_HttpClient.DefaultRequestHeaders.Add("x-goog-meta-matdesc", encryptionMetadata.matDesc);
		m_HttpClient.DefaultRequestHeaders.Add("x-goog-meta-encryptiondata", encryptionData);

		// Convert file bytes to stream
		var strm = new StreamContent(new MemoryStream(fileBytes));
		// Set the stream content type
		strm.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

		try
		{
			// Issue the POST/PUT request
			var response = m_HttpClient.Put(fileMetadata.presignedUrl, strm);
		}
		catch (HttpRequestException err)
		{
			fileMetadata.lastError = err;
			if (err.Message.Contains("400") && GCS_ACCESS_TOKEN != null)
			{
				fileMetadata.resultStatus = ResultStatus.RENEW_PRESIGNED_URL.ToString();
			}
			else if (err.Message.Contains("401"))
			{
				fileMetadata.resultStatus = ResultStatus.RENEW_TOKEN.ToString();
			}
			else if (err.Message.Contains("403") ||
				err.Message.Contains("500") ||
				err.Message.Contains("503"))
			{
				fileMetadata.resultStatus = ResultStatus.NEED_RETRY.ToString();
			}
			return;
		}

		fileMetadata.destFileSize = fileMetadata.uploadSize;
		fileMetadata.resultStatus = ResultStatus.UPLOADED.ToString();
	}

	/// <summary>
	/// Download the file to the local location.
	/// </summary>
	/// <param name="fileMetadata">The GCS file metadata.</param>
	/// <param name="fullDstPath">The local location to store downloaded file into.</param>
	/// <param name="maxConcurrency">Number of max concurrency.</param>
	public void DownloadFile(SFFileMetadata fileMetadata, string fullDstPath, int maxConcurrency)
	{
		try
		{
			// Issue the POST/PUT request
			var response = m_HttpClient.Get(fileMetadata.presignedUrl);

			// Write to file
			using (var fileStream = File.Create(fullDstPath))
			{
				var responseTask = response.Content.ReadAsStreamAsync();
				responseTask.Wait();

				responseTask.Result.CopyTo(fileStream);
			}

			var headers = response.Headers;

			// Get header values
			dynamic? encryptionData = null;
			if (headers.TryGetValues(GCS_METADATA_ENCRYPTIONDATAPROP, out var values1))
			{
				encryptionData = JsonConvert.DeserializeObject(values1.First());
			}

			string? matDesc = null;
			if (headers.TryGetValues(GCS_METADATA_MATDESC_KEY, out var values2))
			{
				matDesc = values2.First();
			}

			// Get encryption metadata from encryption data header value
			SFEncryptionMetadata? encryptionMetadata = null;
			if (encryptionData != null)
			{
				encryptionMetadata = new SFEncryptionMetadata
				{
					iv = encryptionData["ContentEncryptionIV"],
					key = encryptionData["WrappedContentKey"]["EncryptedKey"],
					matDesc = matDesc
				};
				fileMetadata.encryptionMetadata = encryptionMetadata;
			}

			if (headers.TryGetValues(GCS_METADATA_SFC_DIGEST, out var values3))
			{
				fileMetadata.sha256Digest = values3.First();
			}

			if (headers.TryGetValues(GCS_FILE_HEADER_CONTENT_LENGTH, out var values4))
			{
				fileMetadata.srcFileSize = (long)Convert.ToDouble(values4.First());
			}
		}
		catch (HttpRequestException err)
		{
			fileMetadata.lastError = err;
			if (err.Message.Contains("401"))
			{
				fileMetadata.resultStatus = ResultStatus.RENEW_TOKEN.ToString();
			}
			else if (err.Message.Contains("403") ||
				err.Message.Contains("500") ||
				err.Message.Contains("503"))
			{
				fileMetadata.resultStatus = ResultStatus.NEED_RETRY.ToString();
			}
			return;
		}

		fileMetadata.resultStatus = ResultStatus.DOWNLOADED.ToString();
	}
}
