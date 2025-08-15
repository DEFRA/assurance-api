using MongoDB.Driver;

namespace AssuranceApi.Data
{
    internal class MongoDbHelpers
    {
        internal static Collation GetCaseInsensitiveCollation()
        {
            return new Collation(
                "en",
                strength: CollationStrength.Secondary,
                caseLevel: true);
        }
    }
}
