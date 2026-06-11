namespace GlamourChecker.Core;

public static class FeatureFlags
{
    /// <summary>
    /// Experimental: Uses an external dictionary of visually identical models 
    /// to group identical items that have different internal Model IDs (e.g. Goatskin vs Warlock).
    /// </summary>
    public static readonly bool EnableVisualDictionary = true;
}
