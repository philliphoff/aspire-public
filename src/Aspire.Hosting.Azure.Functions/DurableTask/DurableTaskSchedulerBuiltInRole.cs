// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Azure.DurableTask;

/// <summary>
/// Represents built-in Azure Durable Task Scheduler roles that can be assigned to identities.
/// </summary>
public readonly struct DurableTaskSchedulerBuiltInRole : IEquatable<DurableTaskSchedulerBuiltInRole>
{
    private readonly string _value;

    private DurableTaskSchedulerBuiltInRole(string value)
    {
        _value = value;
    }

    /// <summary>
    /// Durable Task Data Contributor role - provides full access to durable task data operations.
    /// Role ID: 46150c50-a455-46b1-bd48-84c5c87c09ab
    /// </summary>
    public static DurableTaskSchedulerBuiltInRole DurableTaskDataContributor { get; } = new("46150c50-a455-46b1-bd48-84c5c87c09ab");

    /// <summary>
    /// Durable Task Data Owner role - provides full access to durable task data operations including management.
    /// Role ID: 5a05c28b-f393-4dd7-bf56-6fa1958e0eba
    /// </summary>
    public static DurableTaskSchedulerBuiltInRole DurableTaskDataOwner { get; } = new("5a05c28b-f393-4dd7-bf56-6fa1958e0eba");

    /// <summary>
    /// Durable Task Data Reader role - provides read-only access to durable task data.
    /// Role ID: 89199220-a9a4-4ebb-811f-c9cfd7e9e826
    /// </summary>
    public static DurableTaskSchedulerBuiltInRole DurableTaskDataReader { get; } = new("89199220-a9a4-4ebb-811f-c9cfd7e9e826");

    /// <summary>
    /// Gets the role ID as a string.
    /// </summary>
    /// <returns>The role ID.</returns>
    public override string ToString() => _value;

    /// <summary>
    /// Gets the human-readable name for the built-in role.
    /// </summary>
    /// <param name="role">The role to get the name for.</param>
    /// <returns>The human-readable role name.</returns>
    public static string GetBuiltInRoleName(DurableTaskSchedulerBuiltInRole role)
    {
        return role._value switch
        {
            "46150c50-a455-46b1-bd48-84c5c87c09ab" => "Durable_Task_Data_Contributor",
            "5a05c28b-f393-4dd7-bf56-6fa1958e0eba" => "Durable_Task_Data_Owner",
            "89199220-a9a4-4ebb-811f-c9cfd7e9e826" => "Durable_Task_Data_Reader",
            _ => role._value
        };
    }

    /// <inheritdoc/>
    public bool Equals(DurableTaskSchedulerBuiltInRole other) => _value == other._value;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is DurableTaskSchedulerBuiltInRole other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => _value?.GetHashCode() ?? 0;

    /// <summary>
    /// Determines if two <see cref="DurableTaskSchedulerBuiltInRole"/> values are equal.
    /// </summary>
    public static bool operator ==(DurableTaskSchedulerBuiltInRole left, DurableTaskSchedulerBuiltInRole right) => left.Equals(right);

    /// <summary>
    /// Determines if two <see cref="DurableTaskSchedulerBuiltInRole"/> values are not equal.
    /// </summary>
    public static bool operator !=(DurableTaskSchedulerBuiltInRole left, DurableTaskSchedulerBuiltInRole right) => !left.Equals(right);
}
