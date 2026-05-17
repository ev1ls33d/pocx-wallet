using System;
using NBitcoin;
using PocxWallet.Core.Address;

namespace PocxWallet.Core.Wallet;

/// <summary>
/// Provides utility functions for creating PoCX-specific transactions
/// </summary>
public static class Transactions
{
    private static readonly byte[] ASSIGNMENT_MARKER = { 0x50, 0x4F, 0x43, 0x58 }; // "POCX"
    private static readonly byte[] REVOCATION_MARKER = { 0x58, 0x43, 0x4F, 0x50 }; // "XCOP"

    /// <summary>
    /// Creates the OP_RETURN script for a forging assignment.
    /// Delegates forging rights from plot address to forging address.
    /// </summary>
    /// <param name="plotAddress">The address that owns the plot (pocx1q...)</param>
    /// <param name="forgeAddress">The address to delegate forging to (pocx1q...)</param>
    /// <returns>The Script for the OP_RETURN output</returns>
    public static Script CreateForgingAssignmentScript(string plotAddress, string forgeAddress)
    {
        var (_, _, plotHash) = Bech32Encoder.Decode(plotAddress);
        var (_, _, forgeHash) = Bech32Encoder.Decode(forgeAddress);

        byte[] data = new byte[44];
        Buffer.BlockCopy(ASSIGNMENT_MARKER, 0, data, 0, 4);
        Buffer.BlockCopy(plotHash, 0, data, 4, 20);
        Buffer.BlockCopy(forgeHash, 0, data, 24, 20);

        return new Script(OpcodeType.OP_RETURN, Op.GetPushOp(data));
    }

    /// <summary>
    /// Creates the OP_RETURN script for a forging revocation.
    /// Reclaims forging rights back to plot owner.
    /// </summary>
    /// <param name="plotAddress">The address that owns the plot (pocx1q...)</param>
    /// <returns>The Script for the OP_RETURN output</returns>
    public static Script CreateForgingRevocationScript(string plotAddress)
    {
        var (_, _, plotHash) = Bech32Encoder.Decode(plotAddress);

        byte[] data = new byte[24];
        Buffer.BlockCopy(REVOCATION_MARKER, 0, data, 0, 4);
        Buffer.BlockCopy(plotHash, 0, data, 4, 20);

        return new Script(OpcodeType.OP_RETURN, Op.GetPushOp(data));
    }
}
