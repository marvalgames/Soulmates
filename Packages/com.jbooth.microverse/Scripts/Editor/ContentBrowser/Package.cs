using System;

namespace JBooth.MicroVerseCore.Browser
{
    /// <summary>
    /// Used for combining installed collections with the same id
    /// </summary>
    public class Package
    {
        public enum PackageType
        {
            Collection,
            Ad
        }

        public string id;
        public string packName;
        public ContentType contentType;
        public PackageType packageType;

        public Package(string id, string packName, ContentType contentType, PackageType packageType)
        {
            this.id = id;
            this.packName = packName;
            this.contentType = contentType;
            this.packageType = packageType;
        }

        public override bool Equals(object obj)
        {
            return obj is Package package &&
                   id == package.id &&
                   packName == package.packName &&
                   contentType == package.contentType &&
                   packageType == package.packageType;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(id, packName, contentType, packageType);
        }

        /// <summary>
        /// Evaluate if the other package is in the same group. A group is defined by id and name
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool IsInGroup( Package other)
        {
            if (other == null)
                return false;

            return id == other.id && packName == other.packName;
        }
    }
}