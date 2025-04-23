﻿/*
 *    The contents of this file are subject to the Initial
 *    Developer's Public License Version 1.0 (the "License");
 *    you may not use this file except in compliance with the
 *    License. You may obtain a copy of the License at
 *    https://github.com/FirebirdSQL/NETProvider/raw/master/license.txt.
 *
 *    Software distributed under the License is distributed on
 *    an "AS IS" basis, WITHOUT WARRANTY OF ANY KIND, either
 *    express or implied. See the License for the specific
 *    language governing rights and limitations under the License.
 *
 *    All Rights Reserved.
 */

//$Authors = Jiri Cincura (jiri@cincura.net), Jean Ressouche, Rafael Almeida (ralms@ralms.net)

namespace FirebirdSql.EntityFrameworkCore.Firebird.Metadata.Internal;

public static class FbAnnotationNames
{
	public const string Prefix = "Fb:";
	public const string ValueGenerationStrategy = Prefix + nameof(ValueGenerationStrategy);
	public const string HiLoSequenceName = Prefix + nameof(HiLoSequenceName);
	public const string HiLoSequenceSchema = Prefix + nameof(HiLoSequenceSchema);
	public const string SequenceName = Prefix + nameof(SequenceName);
	public const string SequenceSchema = Prefix + nameof(SequenceSchema);
	public const string SequenceNameSuffix = Prefix + nameof(SequenceNameSuffix);

	public const string BlobSegmentSize = Prefix + nameof(BlobSegmentSize);
	public const string CharacterSet = Prefix + nameof(CharacterSet);
	public const string DomainName = Prefix + nameof(DomainName);

	public const string IdentityType = Prefix + nameof(IdentityType);
	public const string IdentityStart = Prefix + nameof(IdentityStart);
	public const string IdentityIncrement = Prefix + nameof(IdentityIncrement);
}
