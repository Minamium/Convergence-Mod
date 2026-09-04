namespace Convergence.Common.Encounters.Abstractions;

internal interface IEncounterTerminationContract
{
    ushort FeatureSchemaId { get; }

    byte FeatureSchemaVersion { get; }

    bool IsValid(in EncounterTerminationDescriptor descriptor);
}
