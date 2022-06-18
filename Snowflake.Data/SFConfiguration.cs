/*
 * Copyright (c) 2012-2019 Snowflake Computing Inc. All rights reserved.
 */

namespace Tortuga.Data.Snowflake;

public record SFConfiguration
{
	public SFConfiguration(bool useV2JsonParser, bool useV2ChunkDownloader, int chunkDownloaderVersion)
	{
		UseV2JsonParser = useV2JsonParser;
		UseV2ChunkDownloader = useV2ChunkDownloader;
		ChunkDownloaderVersion = chunkDownloaderVersion;
	}

	public bool UseV2JsonParser { get; init; }

	// Leave this configuration for backward compatibility.
	// We would discard it after we announce this change.
	// Right now, when this is true, it would disable the ChunkDownloaderVersion
	public bool UseV2ChunkDownloader { get; init; }

	public int ChunkDownloaderVersion { get; init; }

	public static SFConfiguration Default { get; set; } =
		new(useV2JsonParser: true, useV2ChunkDownloader: false, chunkDownloaderVersion: 3);
}
