namespace Assets.Scripts.Content
{
    public static class ContentPolicy
    {
        public static string GetContentExtension(ContentCategory category)
        {
            switch (category)
            {
                case ContentCategory.Data:
                    {
                        return ".json";
                    }
                case ContentCategory.Meta:
                    {
                        return ".meta.json";
                    }
                case ContentCategory.Bundle:
                    {
                        return ".bundle";
                    }
                default:
                    {
                        return string.Empty;
                    }
            }
        }

        public static bool TryGetLocalPathParts(
            ContentEntry entry,
                out string categoryDir,
                out string schemaDir,
                out string optionalSubDir,
                out string fileBaseName,
                out string extension
        )
        {
            categoryDir = string.Empty;
            schemaDir = string.Empty;
            optionalSubDir = string.Empty;
            fileBaseName = string.Empty;
            extension = string.Empty;

            if (entry == null)
            {
                return false;
            }

            if (entry.header.staticKey == 0u)
            {
                return false;
            }

            if (string.IsNullOrEmpty(entry.header.schema))
            {
                return false;
            }

            if (string.IsNullOrEmpty(entry.header.id))
            {
                return false;
            }
            
            
            categoryDir = entry.header.category.ToString();
            schemaDir = entry.header.schema;
            extension = GetContentExtension(entry.header.category);
            if (string.IsNullOrEmpty(extension))
            {
                return false;
            }

            if (entry is ContentBundleEntry bundle)
            {
                if (string.IsNullOrEmpty(bundle.sha256))
                {
                    return false;
                }

                optionalSubDir = entry.header.id;
                fileBaseName = bundle.sha256;
                return true;
            }

            fileBaseName = entry.header.id;
            return true;
        }
    }
}