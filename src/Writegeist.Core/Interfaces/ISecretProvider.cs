namespace Writegeist.Core.Interfaces;

public interface ISecretProvider
{
    /// <summary>
    /// Returns the value for the given key, prompting the user if not already set.
    /// </summary>
    string Require(string key, string description);
}
