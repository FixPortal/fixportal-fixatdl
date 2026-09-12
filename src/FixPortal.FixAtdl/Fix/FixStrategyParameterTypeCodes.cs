namespace FixPortal.FixAtdl.Fix;

/// <summary>
/// Maps a FIXatdl parameter type name (e.g. "Int_t") to its FIX StrategyParameterType (tag 959)
/// code. Mirrors the FIX 4.4 StrategyParameterType field values exactly.
/// </summary>
public static class FixStrategyParameterTypeCodes
{
    /// <summary>Resolves a FIXatdl type name to its FIX StrategyParameterType code.</summary>
    /// <remarks>Unknown types default to 14 (String_t) per FIX's permissive rule: a string carries
    /// any value and lets the counterparty interpret the content without a parse error.</remarks>
    public static int Resolve(string fixatdlType) =>
        fixatdlType switch
        {
            "Int_t" => 1,
            "Length_t" => 2,
            "NumInGroup_t" => 3,
            "SeqNum_t" => 4,
            "TagNum_t" => 5,
            "Float_t" => 6,
            "Qty_t" => 7,
            "Price_t" => 8,
            "PriceOffset_t" => 9,
            "Amt_t" => 10,
            "Percentage_t" => 11,
            "Char_t" => 12,
            "Boolean_t" => 13,
            "String_t" => 14,
            "MultipleCharValue_t" => 15,
            "Currency_t" => 16,
            "Exchange_t" => 17,
            "MonthYear_t" => 18,
            "UTCTimestamp_t" => 19,
            "UTCTimeOnly_t" => 20,
            "LocalMktDate_t" => 21,
            "UTCDateOnly_t" => 22,
            "Data_t" => 23,
            "MultipleStringValue_t" => 24,
            "Country_t" => 25,
            "NumInMsg_t" => 26,
            "TZTimeOnly_t" => 27,
            "TZTimestamp_t" => 28,
            "XMLData_t" => 29,
            "Language_t" => 30,
            _ => 14,
        };
}
