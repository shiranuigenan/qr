using System.Collections;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace QRCoder;

public partial class QRCodeGenerator : IDisposable
{
    public static QRCodeData GenerateQrCode(string plainText)
    {
        int version = 1;

        var segment = new AlphanumericDataSegment(plainText);
        var bitArray = segment.ToBitArray();

        var eccInfo = new ECCInfo(
                      version: 1,
                      totalDataCodewords: 9,
                      eccPerBlock: 17,
                      blocksInGroup1: 1,
                      codewordsInGroup1: 9,
                      blocksInGroup2: 0,
                      codewordsInGroup2: 0
                  );

        // Fill up data code word
        PadData();

        // Calculate error correction blocks
        var codeWordWithECC = CalculateECCBlocks();

        // Calculate interleaved code word lengths
        var interleavedLength = CalculateInterleavedLength();

        // Interleave code words
        var interleavedData = InterleaveData();

        // Place interleaved data on module matrix
        var qrData = PlaceModules();

        CodewordBlock.ReturnList(codeWordWithECC);

        return qrData;

        // fills the bit array with a repeating pattern to reach the required length
        void PadData()
        {
            var dataLength = eccInfo.TotalDataBits;
            var lengthDiff = dataLength - bitArray.Length;
            if (lengthDiff > 0)
            {
                // set 'write index' to end of existing bit array
                var index = bitArray.Length;
                // extend bit array to required length
                bitArray.Length = dataLength;
                // compute padding length
                int padLength = 4;
                // pad with zeros (or less if not enough room)
                index += padLength;
                // pad to nearest 8 bit boundary
                if ((uint)index % 8 != 0)
                    index += 8 - (int)((uint)index % 8);
                // pad with repeating pattern
                var repeatingPatternIndex = 0;
                while (index < dataLength)
                {
                    bitArray[index++] = _repeatingPattern[repeatingPatternIndex++];
                    if (repeatingPatternIndex >= _repeatingPattern.Length)
                        repeatingPatternIndex = 0;
                }
            }
        }

        List<CodewordBlock> CalculateECCBlocks()
        {
            List<CodewordBlock> codewordBlocks;
            // Generate the generator polynomial using the number of ECC words.
            using (var generatorPolynom = CalculateGeneratorPolynom(eccInfo.ECCPerBlock))
            {
                //Calculate error correction words
                codewordBlocks = CodewordBlock.GetList(eccInfo.BlocksInGroup1 + eccInfo.BlocksInGroup2);
                AddCodeWordBlocks(1, eccInfo.BlocksInGroup1, eccInfo.CodewordsInGroup1, 0, bitArray.Length, generatorPolynom);
                int offset = eccInfo.BlocksInGroup1 * eccInfo.CodewordsInGroup1 * 8;
                AddCodeWordBlocks(2, eccInfo.BlocksInGroup2, eccInfo.CodewordsInGroup2, offset, bitArray.Length - offset, generatorPolynom);
                return codewordBlocks;
            }

            void AddCodeWordBlocks(int blockNum, int blocksInGroup, int codewordsInGroup, int offset2, int count, Polynom generatorPolynom)
            {
                _ = blockNum;
                var groupLength = codewordsInGroup * 8;
                groupLength = groupLength > count ? count : groupLength;
                for (var i = 0; i < blocksInGroup; i++)
                {
                    var eccWordList = CalculateECCWords(bitArray, offset2, groupLength, eccInfo, generatorPolynom);
                    codewordBlocks.Add(new CodewordBlock(offset2, groupLength, eccWordList));
                    offset2 += groupLength;
                }
            }
        }

        // Calculate the length of the interleaved data
        int CalculateInterleavedLength()
        {
            var length = 0;
            var codewords = Math.Max(eccInfo.CodewordsInGroup1, eccInfo.CodewordsInGroup2);
            for (var i = 0; i < codewords; i++)
            {
                foreach (var codeBlock in codeWordWithECC)
                    if ((uint)codeBlock.CodeWordsLength / 8 > i)
                        length += 8;
            }
            for (var i = 0; i < eccInfo.ECCPerBlock; i++)
            {
                foreach (var codeBlock in codeWordWithECC)
                    if (codeBlock.ECCWords.Count > i)
                        length += 8;
            }
            return length;
        }

        // Interleave the data
        BitArray InterleaveData()
        {
            var data = new BitArray(interleavedLength);
            int pos = 0;
            for (var i = 0; i < Math.Max(eccInfo.CodewordsInGroup1, eccInfo.CodewordsInGroup2); i++)
            {
                foreach (var codeBlock in codeWordWithECC)
                {
                    if ((uint)codeBlock.CodeWordsLength / 8 > i)
                        pos = bitArray.CopyTo(data, (int)((uint)i * 8) + codeBlock.CodeWordsOffset, pos, 8);
                }
            }
            for (var i = 0; i < eccInfo.ECCPerBlock; i++)
            {
                foreach (var codeBlock in codeWordWithECC)
                    if (codeBlock.ECCWords.Count > i)
                        pos = DecToBin(codeBlock.ECCWords.Array![i], 8, data, pos);
            }

            return data;
        }

        // Place the modules on the QR code matrix
        QRCodeData PlaceModules()
        {
            var qr = new QRCodeData(version, true);
            var size = qr.ModuleMatrix.Count - 8;
            var tempBitArray = new BitArray(18); //version string requires 18 bits
            using (var blockedModules = new ModulePlacer.BlockedModules(size))
            {
                ModulePlacer.PlaceFinderPatterns(qr, blockedModules);
                ModulePlacer.ReserveSeperatorAreas(version, size, blockedModules);
                ModulePlacer.PlaceTimingPatterns(qr, blockedModules);
                ModulePlacer.PlaceDarkModule(qr, version, blockedModules);
                ModulePlacer.ReserveVersionAreas(size, version, blockedModules);
                ModulePlacer.PlaceDataWords(qr, interleavedData, blockedModules);
                var maskVersion = ModulePlacer.MaskCode(qr, version, blockedModules);
                GetFormatString(tempBitArray, version, maskVersion);
                ModulePlacer.PlaceFormat(qr, tempBitArray, true);
            }

            return qr;
        }
    }
    private static readonly BitArray _repeatingPattern = new BitArray(new[] { true, true, true, false, true, true, false, false, false, false, false, true, false, false, false, true });
    private static readonly BitArray _getFormatGenerator = new BitArray(new bool[] { true, false, true, false, false, true, true, false, true, true, true });
    private static readonly BitArray _getFormatMask = new BitArray(new bool[] { true, false, true, false, true, false, false, false, false, false, true, false, false, true, false });
    private static readonly BitArray _getFormatMicroMask = new BitArray(new bool[] { true, false, false, false, true, false, false, false, true, false, false, false, true, false, true });
    private static void GetFormatString(BitArray fStrEcc, int version, int maskVersion)
    {
        fStrEcc.Length = 15;
        fStrEcc.SetAll(false);
        WriteEccLevelAndVersion();

        // Apply the format generator polynomial to add error correction to the format string.
        int index = 0;
        int count = 15;
        TrimLeadingZeros(fStrEcc, ref index, ref count);
        while (count > 10)
        {
            for (var i = 0; i < _getFormatGenerator.Length; i++)
                fStrEcc[index + i] ^= _getFormatGenerator[i];
            TrimLeadingZeros(fStrEcc, ref index, ref count);
        }

        // Align bits with the start of the array.
        ShiftTowardsBit0(fStrEcc, index);

        // Prefix the error correction bits with the ECC level and version number.
        fStrEcc.Length = 10 + 5;
        ShiftAwayFromBit0(fStrEcc, (10 - count) + 5);
        WriteEccLevelAndVersion();

        // XOR the format string with a predefined mask to add robustness against errors.
        fStrEcc.Xor(version < 0 ? _getFormatMicroMask : _getFormatMask);

        void WriteEccLevelAndVersion()
        {
            fStrEcc[0] = true;
            // Insert the 3-bit mask version directly after the error correction level bits.
            DecToBin(maskVersion, 3, fStrEcc, 2);
        }
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void TrimLeadingZeros(BitArray fStrEcc, ref int index, ref int count)
    {
        while (count > 0 && !fStrEcc[index])
        {
            index++;
            count--;
        }
    }
    private static void ShiftTowardsBit0(BitArray fStrEcc, int num)
    {
        for (var i = 0; i < fStrEcc.Length - num; i++)
            fStrEcc[i] = fStrEcc[i + num];
        for (var i = fStrEcc.Length - num; i < fStrEcc.Length; i++)
            fStrEcc[i] = false;
    }
    private static void ShiftAwayFromBit0(BitArray fStrEcc, int num)
    {
        fStrEcc.LeftShift(num); // Shift away from bit 0
    }
    private static ArraySegment<byte> CalculateECCWords(BitArray bitArray, int offset, int count, ECCInfo eccInfo, Polynom generatorPolynomBase)
    {
        var eccWords = eccInfo.ECCPerBlock;
        // Calculate the message polynomial from the bit array data.
        var messagePolynom = CalculateMessagePolynom(bitArray, offset, count);
        var generatorPolynom = generatorPolynomBase.Clone();

        // Adjust the exponents in the message polynomial to account for ECC length.
        for (var i = 0; i < messagePolynom.Count; i++)
            messagePolynom[i] = new PolynomItem(messagePolynom[i].Coefficient,
                messagePolynom[i].Exponent + eccWords);

        // Adjust the generator polynomial exponents based on the message polynomial.
        for (var i = 0; i < generatorPolynom.Count; i++)
            generatorPolynom[i] = new PolynomItem(generatorPolynom[i].Coefficient,
                generatorPolynom[i].Exponent + (messagePolynom.Count - 1));

        // Divide the message polynomial by the generator polynomial to find the remainder.
        var leadTermSource = messagePolynom;
        for (var i = 0; (leadTermSource.Count > 0 && leadTermSource[leadTermSource.Count - 1].Exponent > 0); i++)
        {
            if (leadTermSource[0].Coefficient == 0)  // Simplify the polynomial if the leading coefficient is zero.
            {
                leadTermSource.RemoveAt(0);
                leadTermSource.Add(new PolynomItem(0, leadTermSource[leadTermSource.Count - 1].Exponent - 1));
            }
            else  // Otherwise, perform polynomial reduction using XOR and multiplication with the generator polynomial.
            {
                // Convert the first coefficient to its corresponding alpha exponent unless it's zero.
                // Coefficients that are zero remain zero because log(0) is undefined.
                var index0Coefficient = leadTermSource[0].Coefficient;
                index0Coefficient = index0Coefficient == 0 ? 0 : GaloisField.GetAlphaExpFromIntVal(index0Coefficient);
                var alphaNotation = new PolynomItem(index0Coefficient, leadTermSource[0].Exponent);
                var resPoly = MultiplyGeneratorPolynomByLeadterm(generatorPolynom, alphaNotation, i);
                ConvertToDecNotationInPlace(resPoly);
                var newPoly = XORPolynoms(leadTermSource, resPoly);
                // Free memory used by the previous polynomials.
                resPoly.Dispose();
                leadTermSource.Dispose();
                // Update the message polynomial with the new remainder.
                leadTermSource = newPoly;
            }
        }

        // Free memory used by the generator polynomial.
        generatorPolynom.Dispose();

        // Convert the resulting polynomial into a byte array representing the ECC codewords.
        var ret = new ArraySegment<byte>(new byte[leadTermSource.Count]);
        var array = ret.Array!;

        for (var i = 0; i < leadTermSource.Count; i++)
            array[i] = (byte)leadTermSource[i].Coefficient;

        // Free memory used by the message polynomial.
        leadTermSource.Dispose();

        return ret;
    }
    private static void ConvertToDecNotationInPlace(Polynom poly)
    {
        for (var i = 0; i < poly.Count; i++)
        {
            // Convert the alpha exponent of the coefficient to its decimal value and create a new polynomial item with the updated coefficient.
            poly[i] = new PolynomItem(GaloisField.GetIntValFromAlphaExp(poly[i].Coefficient), poly[i].Exponent);
        }
    }
    private static Polynom CalculateMessagePolynom(BitArray bitArray, int offset, int bitCount)
    {
        // Calculate how many full 8-bit codewords are present
        var fullBytes = bitCount / 8;

        // Determine if there is a remaining partial byte (e.g., 4 bits for Micro QR M1 and M3 versions)
        var remainingBits = bitCount % 8;

        if (remainingBits > 0)
        {
            // Pad the last byte with zero bits to make it a full 8-bit codeword
            var addlBits = 8 - remainingBits;
            var minBitArrayLength = offset + bitCount + addlBits;

            // Extend BitArray length if needed to fit the padded bits
            if (bitArray.Length < minBitArrayLength)
                bitArray.Length = minBitArrayLength;

            // Pad the remaining bits with false (0) values
            for (int i = 0; i < addlBits; i++)
                bitArray[offset + bitCount + i] = false;
        }

        // Total number of codewords (includes extra for partial byte if present)
        var polynomLength = fullBytes + (remainingBits > 0 ? 1 : 0);

        // Initialize the polynomial
        var messagePol = new Polynom(polynomLength);

        // Exponent for polynomial terms starts from highest degree
        int exponent = polynomLength - 1;

        // Convert each 8-bit segment into a decimal value and add it to the polynomial
        for (int i = 0; i < polynomLength; i++)
        {
            messagePol.Add(new PolynomItem(BinToDec(bitArray, offset, 8), exponent--));
            offset += 8;
        }

        return messagePol;
    }
    private static Polynom CalculateGeneratorPolynom(int numEccWords)
    {
        var generatorPolynom = new Polynom(2); // Start with the simplest form of the polynomial
        generatorPolynom.Add(new PolynomItem(0, 1));
        generatorPolynom.Add(new PolynomItem(0, 0));

        using (var multiplierPolynom = new Polynom(numEccWords * 2)) // Used for polynomial multiplication
        {
            for (var i = 1; i <= numEccWords - 1; i++)
            {
                // Clear and set up the multiplier polynomial for the current multiplication
                multiplierPolynom.Clear();
                multiplierPolynom.Add(new PolynomItem(0, 1));
                multiplierPolynom.Add(new PolynomItem(i, 0));

                // Multiply the generator polynomial by the current multiplier polynomial
                var newGeneratorPolynom = MultiplyAlphaPolynoms(generatorPolynom, multiplierPolynom);
                generatorPolynom.Dispose();
                generatorPolynom = newGeneratorPolynom;
            }
        }

        return generatorPolynom; // Return the completed generator polynomial
    }
    private static int BinToDec(BitArray bitArray, int offset, int count)
    {
        var ret = 0;
        for (int i = 0; i < count; i++)
        {
            ret ^= bitArray[offset + i] ? 1 << (count - i - 1) : 0;
        }
        return ret;
    }
    private static int DecToBin(int decNum, int bits, BitArray bitList, int index)
    {
        // Convert decNum to binary using a bitwise operation
        for (int i = bits - 1; i >= 0; i--)
        {
            // Check each bit from most significant to least significant
            bool bit = (decNum & (1 << i)) != 0;
            bitList[index++] = bit;
        }
        return index;
    }
    private static Polynom XORPolynoms(Polynom messagePolynom, Polynom resPolynom)
    {
        // Determine the larger of the two polynomials to guide the XOR operation.
        var resultPolynom = new Polynom(Math.Max(messagePolynom.Count, resPolynom.Count) - 1);
        Polynom longPoly, shortPoly;
        if (messagePolynom.Count >= resPolynom.Count)
        {
            longPoly = messagePolynom;
            shortPoly = resPolynom;
        }
        else
        {
            longPoly = resPolynom;
            shortPoly = messagePolynom;
        }

        // XOR the coefficients of the two polynomials.
        for (var i = 1; i < longPoly.Count; i++)
        {
            var polItemRes = new PolynomItem(
                longPoly[i].Coefficient ^
                (shortPoly.Count > i ? shortPoly[i].Coefficient : 0),
                messagePolynom[0].Exponent - i
            );
            resultPolynom.Add(polItemRes);
        }

        return resultPolynom;
    }
    private static Polynom MultiplyGeneratorPolynomByLeadterm(Polynom genPolynom, PolynomItem leadTerm, int lowerExponentBy)
    {
        var resultPolynom = new Polynom(genPolynom.Count);
        foreach (var polItemBase in genPolynom)
        {
            var polItemRes = new PolynomItem(

                (polItemBase.Coefficient + leadTerm.Coefficient) % 255,
                polItemBase.Exponent - lowerExponentBy
            );
            resultPolynom.Add(polItemRes);
        }
        return resultPolynom;
    }
    private static Polynom MultiplyAlphaPolynoms(Polynom polynomBase, Polynom polynomMultiplier)
    {
        // Initialize a new polynomial with a size based on the product of the sizes of the two input polynomials.
        var resultPolynom = new Polynom(polynomMultiplier.Count * polynomBase.Count);

        // Multiply each term of the first polynomial by each term of the second polynomial.
        foreach (var polItemBase in polynomMultiplier)
        {
            foreach (var polItemMulti in polynomBase)
            {
                // Create a new polynomial term with the coefficients added (as exponents) and exponents summed.
                var polItemRes = new PolynomItem
                (
                    GaloisField.ShrinkAlphaExp(polItemBase.Coefficient + polItemMulti.Coefficient),
                    (polItemBase.Exponent + polItemMulti.Exponent)
                );
                resultPolynom.Add(polItemRes);
            }
        }

        // Identify and merge terms with the same exponent.
        var toGlue = GetNotUniqueExponents(resultPolynom, resultPolynom.Count <= 128 ? stackalloc int[128].Slice(0, resultPolynom.Count) : new int[resultPolynom.Count]);
        var gluedPolynoms = toGlue.Length <= 128
            ? stackalloc PolynomItem[128].Slice(0, toGlue.Length)
            : new PolynomItem[toGlue.Length];

        var gluedPolynomsIndex = 0;
        foreach (var exponent in toGlue)
        {
            var coefficient = 0;
            foreach (var polynomOld in resultPolynom)
            {
                if (polynomOld.Exponent == exponent)
                    coefficient ^= GaloisField.GetIntValFromAlphaExp(polynomOld.Coefficient);
            }

            // Fix the polynomial terms by recalculating the coefficients based on XORed results.
            var polynomFixed = new PolynomItem(GaloisField.GetAlphaExpFromIntVal(coefficient), exponent);
            gluedPolynoms[gluedPolynomsIndex++] = polynomFixed;
        }

        // Remove duplicated exponents and add the corrected ones back.
        for (int i = resultPolynom.Count - 1; i >= 0; i--)
            if (toGlue.Contains(resultPolynom[i].Exponent))
                resultPolynom.RemoveAt(i);
        foreach (var polynom in gluedPolynoms)
            resultPolynom.Add(polynom);

        // Sort the polynomial terms by exponent in descending order.
        resultPolynom.Sort((x, y) => -x.Exponent.CompareTo(y.Exponent));
        return resultPolynom;

        // Auxiliary function to identify exponents that appear more than once in the polynomial.
        static ReadOnlySpan<int> GetNotUniqueExponents(Polynom list, Span<int> buffer)
        {
            // It works as follows:
            // 1. a scratch buffer of the same size as the list is passed in
            // 2. exponents are written / copied to that scratch buffer
            // 3. scratch buffer is sorted, thus the exponents are in order
            // 4. for each item in the scratch buffer (= ordered exponents) it's compared w/ the previous one
            //   * if equal, then increment a counter
            //   * else check if the counter is $>0$ and if so write the exponent to the result
            // 
            // For writing the result the same scratch buffer is used, as by definition the index to write the result 
            // is `<=` the iteration index, so no overlap, etc. can occur.

            Debug.Assert(list.Count == buffer.Length);

            int idx = 0;
            foreach (var row in list)
            {
                buffer[idx++] = row.Exponent;
            }

            buffer.Sort();

            idx = 0;
            int expCount = 0;
            int last = buffer[0];

            for (int i = 1; i < buffer.Length; ++i)
            {
                if (buffer[i] == last)
                {
                    expCount++;
                }
                else
                {
                    if (expCount > 0)
                    {
                        Debug.Assert(idx <= i - 1);

                        buffer[idx++] = last;
                        expCount = 0;
                    }
                }

                last = buffer[i];
            }

            return buffer.Slice(0, idx);
        }
    }
    public virtual void Dispose()
    {
        // left for back-compat
        GC.SuppressFinalize(this);
    }
}
