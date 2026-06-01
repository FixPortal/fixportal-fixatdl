// FP Enhancement: 2026-05-24 — modernised for net10 (file-scoped, nullable, FixPortal namespace).
#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

using FixPortal.FixAtdl.Fix;
using FixPortal.FixAtdl.Model.Collections;
using FixPortal.FixAtdl.Model.Controls;
using FixPortal.FixAtdl.Model.Elements;
using FixPortal.FixAtdl.Model.Elements.Support;
using FixPortal.FixAtdl.Model.Reference;
using FixPortal.FixAtdl.Model.Types;
using FixPortal.FixAtdl.Model.Types.Support;
using FixPortal.FixAtdl.Xml.Serialization;
using Country_t = FixPortal.FixAtdl.Model.Elements.Country_t;

namespace FixPortal.FixAtdl.Xml;

/// <summary>
/// Provides the definition of the FIXatdl schema.
/// </summary>
public static class SchemaDefinitions
{
    #region SecurityType_t Definition

    private static readonly ElementAttribute[] SecurityTypeAttributes =
    [
        new("name", "Name", typeof(string), Required.Mandatory),
        new("inclusion", "Inclusion", EnumDefinitions.Inclusion_t, Required.Mandatory)
    ];

    private static readonly ElementDefinition SecurityType_t = new(
        AtdlNamespaces.core + "SecurityType", typeof(SecurityType_t), SecurityTypeAttributes);

    private static readonly ContainerElementDefinition SecurityTypes = new(
        AtdlNamespaces.core + "SecurityTypes", SecurityType_t);

    #endregion // SecurityType_t Definition

    #region Market_t Definition

    private static readonly ElementAttribute[] MarketAttributes =
    [
        new("MICCode", "MICCode", typeof(string), Required.Mandatory),
        new("inclusion", "Inclusion", EnumDefinitions.Inclusion_t, Required.Mandatory)
    ];

    private static readonly ElementDefinition Market_t = new(
        AtdlNamespaces.core + "Market", typeof(Market_t), MarketAttributes);

    private static readonly ContainerElementDefinition Markets = new(
        AtdlNamespaces.core + "Markets", Market_t);

    #endregion // Market_t Definition

    #region Region_t & Country_t Definition

    private static readonly ElementAttribute[] CountryAttributes =
    [
        new("CountryCode", "CountryCode", typeof(IsoCountryCode), Required.Mandatory),
        new("inclusion", "Inclusion", EnumDefinitions.Inclusion_t, Required.Mandatory)
    ];

    private static readonly ElementDefinition Country_t = new(
        AtdlNamespaces.core + "Country", typeof(Country_t), CountryAttributes);

    private static readonly ElementAttribute[] RegionAttributes =
    [
        new("name", "Name", EnumDefinitions.Region, Required.Mandatory),
        new("inclusion", "Inclusion", EnumDefinitions.Inclusion_t, Required.Mandatory)
    ];

    private static readonly ElementDefinition Region_t = new(
        AtdlNamespaces.core + "Region", typeof(Region_t), RegionAttributes,
        new ChildElementDefinition(Country_t, "Countries",
                typeof(CountryCollection), StandardContainerMethod.Add));

    private static readonly ContainerElementDefinition Regions = new(
        AtdlNamespaces.core + "Regions", Region_t);

    #endregion // Region_t & Country_t Definition

    #region Parameter_t Definition

    private static readonly ConstructorParameter[] ParameterConstructorParameters =
    [
        new(typeof(string), SourceType.ElementAttribute, "name")
    ];

    private static readonly ElementAttribute[] ParameterCommonAttributes =
    [
        new("definedByFIX", "DefinedByFix", typeof(bool), Required.Optional),
        new("fixTag", "FixTag", typeof(FixTag), Required.Optional),
        new("mutableOnCxlRpl", "MutableOnCxlRpl", typeof(bool), Required.Optional),
        new("revertOnCxlRpl", "RevertOnCxlRpl", typeof(bool), Required.Optional),
        new("use", "Use", EnumDefinitions.Use_t, Required.Optional),
    ];

    private static readonly ElementAttribute[] BooleanDefinition =
    [
        new("constValue", "Value.ConstValue", typeof(bool), Required.Optional),
        new("falseWireValue", "Value.FalseWireValue", typeof(string), Required.Optional),
        new("trueWireValue", "Value.TrueWireValue", typeof(string), Required.Optional)
    ];

    private static readonly ElementAttribute[] CharDefinition =
    [
        new("constValue", "Value.ConstValue", typeof(char), Required.Optional),
    ];

    private static readonly ElementAttribute[] CountryDefinition =
    [
        new("constValue", "Value.ConstValue", typeof(IsoCountryCode), Required.Optional),
        new("maxLength", "Value.MaxLength", typeof(int), Required.Optional),
        new("minLength", "Value.MinLength", typeof(int), Required.Optional),
    ];

    private static readonly ElementAttribute[] CurrencyDefinition =
    [
        new("constValue", "Value.ConstValue", typeof(IsoCurrencyCode), Required.Optional),
    ];

    private static readonly ElementAttribute[] DataDefinition =
    [
        new("constValue", "Value.ConstValue", typeof(char[]), Required.Optional),
        new("maxLength", "Value.MaxLength", typeof(int), Required.Optional),
        new("minLength", "Value.MinLength", typeof(int), Required.Optional),
    ];

    private static readonly ElementAttribute[] ExchangeDefinition =
    [
        new("constValue", "Value.ConstValue", typeof(string), Required.Optional)
    ];

    /// <remarks>Used for Amt_t, Float_t, Price_t, PriceOffset_t, Qty_t.  (Percentage_t has an extra attribute.)</remarks>
    private static readonly ElementAttribute[] FloatDefinition =
    [
        new("constValue", "Value.ConstValue", typeof(decimal), Required.Optional),
        new("maxValue", "Value.MaxValue", typeof(decimal), Required.Optional),
        new("minValue", "Value.MinValue", typeof(decimal), Required.Optional),
        new("precision", "Value.Precision", typeof(int), Required.Optional)
    ];

    private static readonly ElementAttribute[] IntDefinition =
    [
        new("constValue", "Value.ConstValue", typeof(int), Required.Optional),
        new("maxValue", "Value.MaxValue", typeof(int), Required.Optional),
        new("minValue", "Value.MinValue", typeof(int), Required.Optional)
    ];

    private static readonly ElementAttribute[] LanguageDefinition =
    [
        new("constValue", "Value.ConstValue", typeof(IsoLanguageCode), Required.Optional)
    ];

    // Used for Length_t, NumInGroup_t, SeqNum_t, TagNum_t
    private static readonly ElementAttribute[] LengthDefinition =
    [
        new("constValue", "Value.ConstValue", typeof(int), Required.Optional),
    ];

    private static readonly ElementAttribute[] LocalMktDateDefinition =
    [
        new("constValue", "Value.ConstValue", typeof(DateTime), Required.Optional),
        new("maxValue", "Value.MaxValue", typeof(DateTime), Required.Optional),
        new("minValue", "Value.MinValue", typeof(DateTime), Required.Optional)
    ];

    private static readonly ElementAttribute[] MonthYearDefinition =
    [
        new("constValue", "Value.ConstValue", typeof(MonthYear), Required.Optional),
        new("maxValue", "Value.MaxValue", typeof(MonthYear), Required.Optional),
        new("minValue", "Value.MinValue", typeof(MonthYear), Required.Optional)
    ];

    private static readonly ElementAttribute[] PercentageDefinition =
    [
        new("constValue", "Value.ConstValue", typeof(decimal), Required.Optional),
        new("maxValue", "Value.MaxValue", typeof(decimal), Required.Optional),
        new("minValue", "Value.MinValue", typeof(decimal), Required.Optional),
        new("precision", "Value.Precision", typeof(int), Required.Optional),
        new("multiplyBy100", "Value.MultiplyBy100", typeof(bool), Required.Optional),
    ];

    // Used for MultipleCharValue_t, MultipleStringValue_t
    private static readonly ElementAttribute[] MultipleStringValueDefinition =
    [
        new("constValue", "Value.ConstValue", typeof(string), Required.Optional),
        new("maxLength", "Value.MaxLength", typeof(int), Required.Optional),
        new("minLength", "Value.MinLength", typeof(int), Required.Optional),
        new("invertOnWire", "Value.InvertOnWire", typeof(bool), Required.Optional),
    ];

    private static readonly ElementAttribute[] StringDefinition =
    [
        new("constValue", "Value.ConstValue", typeof(string), Required.Optional),
        new("maxLength", "Value.MaxLength", typeof(int), Required.Optional),
        new("minLength", "Value.MinLength", typeof(int), Required.Optional),
    ];

    private static readonly ElementAttribute[] TenorDefinition =
    [
        new("constValue", "Value.ConstValue", typeof(Tenor), Required.Optional)
    ];

    // Used for TZTimeOnly_t, TZTimestamp_t, UTCDateOnly_t, UTCTimeOnly_t
    // (UTCTimestamp_t has an extra attribute.)
    private static readonly ElementAttribute[] TZTimeOnlyDefinition =
    [
        new("constValue", "Value.ConstValue", typeof(DateTime), Required.Optional),
        new("maxValue", "Value.MaxValueText", typeof(string), Required.Optional),
        new("minValue", "Value.MinValueText", typeof(string), Required.Optional)
    ];

    private static readonly ElementAttribute[] UTCTimestampDefinition =
    [
        new("constValue", "Value.ConstValue", typeof(DateTime), Required.Optional),
        new("maxValue", "Value.MaxValueText", typeof(string), Required.Optional),
        new("minValue", "Value.MinValueText", typeof(string), Required.Optional),
        new("localMktTz", "Value.LocalMktTz", typeof(string), Required.Optional)
    ];

    private static readonly ChildElementDefinition EnumPairs = new(
        new ElementDefinition(AtdlNamespaces.core + "EnumPair", typeof(EnumPair_t),
            [
                new ElementAttribute("enumID", "EnumId", typeof(string), Required.Mandatory),
                new ElementAttribute("wireValue", "WireValue", typeof(string), Required.Mandatory),
                new ElementAttribute("index", "Index", typeof(int), Required.Optional)
            ]),
            "EnumPairs", typeof(EnumPairCollection), StandardContainerMethod.Add);

    /// <summary>
    /// Defines the content of Parameter_t.
    /// </summary>
    public static readonly GenericTypeElementDefinition Parameter_t = new(
        AtdlNamespaces.core + "Parameter", typeof(Parameter_t<>), AtdlNamespaces.xsi + "type", "FixPortal.FixAtdl.Model.Types",
        ParameterConstructorParameters, ParameterCommonAttributes,
        new Dictionary<Type, ElementAttribute[]>
        {
                {  typeof(Amt_t), FloatDefinition },
                {  typeof(Boolean_t), BooleanDefinition },
                {  typeof(Char_t), CharDefinition },
                {  typeof(Model.Types.Country_t), CountryDefinition },
                {  typeof(Currency_t), CurrencyDefinition },
                {  typeof(Data_t), DataDefinition },
                {  typeof(Exchange_t), ExchangeDefinition },
                {  typeof(Float_t), FloatDefinition },
                {  typeof(Int_t), IntDefinition },
                {  typeof(Language_t), LanguageDefinition },
                {  typeof(Length_t), LengthDefinition },
                {  typeof(LocalMktDate_t), LocalMktDateDefinition },
                {  typeof(MonthYear_t), MonthYearDefinition },
                {  typeof(MultipleCharValue_t), MultipleStringValueDefinition },
                {  typeof(MultipleStringValue_t), MultipleStringValueDefinition },
                {  typeof(NumInGroup_t), LengthDefinition },
                {  typeof(Percentage_t), PercentageDefinition },
                {  typeof(Price_t), FloatDefinition },
                {  typeof(PriceOffset_t), FloatDefinition },
                {  typeof(Qty_t), FloatDefinition },
                {  typeof(SeqNum_t), LengthDefinition },
                {  typeof(String_t), StringDefinition },
                {  typeof(TagNum_t), LengthDefinition },
                {  typeof(Tenor_t), TenorDefinition },
                {  typeof(TZTimeOnly_t), TZTimeOnlyDefinition },
                {  typeof(TZTimestamp_t), TZTimeOnlyDefinition },
                {  typeof(UTCDateOnly_t), TZTimeOnlyDefinition },
                {  typeof(UTCTimeOnly_t), TZTimeOnlyDefinition },
                {  typeof(UTCTimestamp_t), UTCTimestampDefinition }
        },
        [EnumPairs]);

    #endregion // Parameter_t Definition

    #region EditRef_t<T> Definitions

    private static readonly ElementAttribute[] EditRefAttributes =
    [
        new("id", "Id", typeof(string), Required.Mandatory)
    ];

    /// <summary>
    /// Defines the content of EditRef_t when it relates to a control.
    /// </summary>
    public static readonly ElementDefinition EditRef_t_Control_t = new(
        AtdlNamespaces.val + "EditRef", typeof(EditRef_t<Control_t>), EditRefAttributes);

    /// <summary>
    /// Defines the content of EditRef_t when it relates to a parameter.
    /// </summary>
    public static readonly ElementDefinition EditRef_t_IParameter_t = new(
        AtdlNamespaces.val + "EditRef", typeof(EditRef_t<IParameter>), EditRefAttributes);

    #endregion // EditRef_t<T> Definitions

    #region Edit_t Definitions

    private static readonly ElementAttribute[] EditAttributes =
    [
        new("field", "Field", typeof(string), Required.Optional),
        new("field2", "Field2", typeof(string), Required.Optional),
        new("id", "Id", typeof(string), Required.Optional),
        new("logicOperator", "LogicOperator", EnumDefinitions.LogicOperator_t, Required.Optional),
        new("operator", "Operator", EnumDefinitions.Operator_t, Required.Optional),
        new("value", "Value", typeof(string), Required.Optional)
    ];

    /// <summary>
    /// Defines the content of Edit_t.
    /// </summary>
    public static readonly ElementDefinition Edit_t = new(
        AtdlNamespaces.val + "Edit", typeof(Edit_t), EditAttributes,
        [
            new ChildElementDefinition(new RecursiveTypeElementDefinition(), "Edits",
                typeof(EditCollection), StandardContainerMethod.Add)
        ]);

    private static readonly ElementDefinition Edit_t_Control_t = new(
        AtdlNamespaces.val + "Edit", typeof(Edit_t<Control_t>), EditAttributes,
        [
            new ChildElementDefinition(new RecursiveTypeElementDefinition(), "Edits",
                typeof(EditEvaluatingCollection<Control_t>), StandardContainerMethod.Add),
            new ChildElementDefinition(EditRef_t_Control_t, "EditRefs",
                typeof(EditRefCollection<Control_t>), StandardContainerMethod.Add)
        ]);

    private static readonly ElementDefinition Edit_t_IParameter_t = new(
        AtdlNamespaces.val + "Edit", typeof(Edit_t<IParameter>), EditAttributes,
        [
            new ChildElementDefinition(new RecursiveTypeElementDefinition(), "Edits",
                typeof(EditEvaluatingCollection<IParameter>), StandardContainerMethod.Add),
            new ChildElementDefinition(EditRef_t_IParameter_t, "EditRefs",
                typeof(EditRefCollection<IParameter>), StandardContainerMethod.Add)
        ]);

    #endregion // Edit_t Definitions

    #region StateRule_t Definition

    private static readonly ElementAttribute[] StateRuleAttibutes =
    [
        new("enabled", "Enabled", typeof(bool), Required.Optional),
        new("visible", "Visible", typeof(bool), Required.Optional),
        new("value", "Value", typeof(string), Required.Optional)
    ];

    /// <summary>
    /// Defines the content of StateRule_t.
    /// </summary>
    public static readonly ElementDefinition StateRule_t = new(
        AtdlNamespaces.flow + "StateRule", typeof(StateRule_t), StateRuleAttibutes,
            [
                new ChildElementDefinition(Edit_t_Control_t, "Edit", typeof(Edit_t<Control_t>), StandardContainerMethod.Assign),
                new ChildElementDefinition(EditRef_t_Control_t, "EditRef", typeof(EditRef_t<Control_t>), StandardContainerMethod.Assign)
            ]);

    #endregion // StateRule_t Definition

    #region ListItem_t Definition

    private static readonly ElementAttribute[] ListItemAttibutes =
    [
        new("uiRep", "UiRep", typeof(string), Required.Mandatory),
        new("enumID", "EnumId", typeof(string), Required.Mandatory)
    ];

    /// <summary>
    /// Defines the content of ListItem_t.
    /// </summary>
    public static readonly ElementDefinition ListItem_t = new(
        AtdlNamespaces.lay + "ListItem", typeof(ListItem_t), ListItemAttibutes);

    #endregion // ListItem_t Definition

    #region Control_t Definition

    private static readonly ElementAttribute[] ControlCommonAttributes =
    [
        new("disableForTemplate","DisableForTemplate", typeof(bool), Required.Optional),
        new("initFixField","InitFixField", typeof(string), Required.Optional),
        new("initPolicy","InitPolicy", EnumDefinitions.InitPolicy_t, Required.Optional),
        new("label", "Label", typeof(string), Required.Optional),
        new("parameterRef","ParameterRef", typeof(string), Required.Optional),
        new("tooltip","ToolTip", typeof(string), Required.Optional)
    ];

    private static readonly ElementAttribute[] CheckBoxAttributes =
    [
        new("checkedEnumRef", "CheckedEnumRef", typeof(string), Required.Optional),
        new("uncheckedEnumRef", "UncheckedEnumRef", typeof(string), Required.Optional),
        new("initValue", "InitValue", typeof(bool), Required.Optional)
    ];

    private static readonly ElementAttribute[] CheckBoxListAttributes =
    [
        new("initValue", "InitValue", typeof(string), Required.Optional),
        new("orientation", "Orientation", EnumDefinitions.Orientation_t, Required.Optional)
    ];

    private static readonly ElementAttribute[] ClockAttributes =
    [
        new("initValue", "InitValue", typeof(InitValueClock), Required.Optional),
        new("initValueMode", "InitValueMode", typeof(int), Required.Optional),
        new("localMktTz", "LocalMktTz", typeof(string), Required.Optional)
    ];

    private static readonly ElementAttribute[] DoubleSpinnerAttributes =
    [
        new("initValue", "InitValue", typeof(decimal), Required.Optional),
        new("innerIncrement", "InnerIncrement", typeof(decimal), Required.Optional),
        new("innerIncrementPolicy", "InnerIncrementPolicy", EnumDefinitions.IncrementPolicy_t, Required.Optional),
        new("outerIncrement", "OuterIncrement", typeof(decimal), Required.Optional),
        new("outerIncrementPolicy", "OuterIncrementPolicy", EnumDefinitions.IncrementPolicy_t, Required.Optional)
    ];

    private static readonly ElementAttribute[] DropDownListAttributes =
    [
        new("initValue", "InitValue", typeof(string), Required.Optional)
    ];

    private static readonly ElementAttribute[] EditableDropDownListAttributes =
    [
        new("initValue", "InitValue", typeof(string), Required.Optional)
    ];

    private static readonly ElementAttribute[] HiddenFieldAttributes =
    [
        new("initValue", "InitValue", typeof(object), Required.Optional)
    ];

    private static readonly ElementAttribute[] LabelAttributes =
    [
        new("initValue", "InitValue", typeof(string), Required.Optional)
    ];

    private static readonly ElementAttribute[] MultiSelectListAttributes =
    [
        new("initValue", "InitValue", typeof(string), Required.Optional)
    ];

    private static readonly ElementAttribute[] RadioButtonAttributes =
    [
        new("initValue", "InitValue", typeof(bool), Required.Optional),
        new("checkedEnumRef", "CheckedEnumRef", typeof(string), Required.Optional),
        new("uncheckedEnumRef", "UncheckedEnumRef", typeof(string), Required.Optional),
        new("radioGroup", "RadioGroup", typeof(string), Required.Optional)
    ];

    private static readonly ElementAttribute[] RadioButtonListAttributes =
    [
        new("initValue", "InitValue", typeof(string), Required.Optional),
        new("orientation", "Orientation", EnumDefinitions.Orientation_t, Required.Optional)
    ];

    private static readonly ElementAttribute[] SingleSelectListAttributes =
    [
        new("initValue", "InitValue", typeof(string), Required.Optional)
    ];

    private static readonly ElementAttribute[] SingleSpinnerAttributes =
    [
        new("initValue", "InitValue", typeof(decimal), Required.Optional),
        new("increment", "Increment", typeof(decimal), Required.Optional),
        new("incrementPolicy", "IncrementPolicy", EnumDefinitions.IncrementPolicy_t, Required.Optional)
    ];

    private static readonly ElementAttribute[] SliderAttributes =
    [
        new("initValue", "InitValue", typeof(string), Required.Optional)
    ];

    private static readonly ElementAttribute[] TextFieldAttributes =
    [
        new("initValue", "InitValue", typeof(string), Required.Optional)
    ];

    /// <summary>
    /// Defines the content of Control_t.
    /// </summary>
    public static readonly MultiTypeElementDefinition Control_t = new(
        AtdlNamespaces.lay + "Control", AtdlNamespaces.xsi + "type", "FixPortal.FixAtdl.Model.Controls",
        [new ConstructorParameter(typeof(string), SourceType.ElementAttribute, "ID")],
        ControlCommonAttributes,
        new Dictionary<Type, ElementAttribute[]>
        {
            { typeof(CheckBox_t), CheckBoxAttributes },
            { typeof(CheckBoxList_t), CheckBoxListAttributes },
            { typeof(Clock_t), ClockAttributes },
            { typeof(DoubleSpinner_t), DoubleSpinnerAttributes },
            { typeof(DropDownList_t), DropDownListAttributes },
            { typeof(EditableDropDownList_t), EditableDropDownListAttributes },
            { typeof(HiddenField_t), HiddenFieldAttributes },
            { typeof(Label_t), LabelAttributes },
            { typeof(MultiSelectList_t), MultiSelectListAttributes },
            { typeof(RadioButton_t), RadioButtonAttributes },
            { typeof(RadioButtonList_t), RadioButtonListAttributes },
            { typeof(SingleSelectList_t), SingleSelectListAttributes },
            { typeof(SingleSpinner_t), SingleSpinnerAttributes },
            { typeof(Slider_t), SliderAttributes },
            { typeof(TextField_t), TextFieldAttributes }
        },
        [
            new ChildElementDefinition(ListItem_t, "ListItems", typeof(ListItemCollection), StandardContainerMethod.Add),
            new ChildElementDefinition(StateRule_t, "StateRules", typeof(StateRuleCollection), StandardContainerMethod.Add)
        ]);

    #endregion // Control_t Definition

    #region StrategyPanel_t Definition

    private static readonly ElementAttribute[] StrategyPanelAttributes =
    [
        new("border", "Border", EnumDefinitions.Border_t, Required.Optional),
        new("collapsed", "Collapsed", typeof(bool), Required.Optional),
        new("collapsible", "Collapsible", typeof(bool), Required.Optional),
        new("color", "Color", typeof(string), Required.Optional),
        new("orientation", "Orientation", EnumDefinitions.Orientation_t, Required.Optional),
        new("title", "Title", typeof(string), Required.Optional)
    ];

    /// <summary>
    /// Defines the content of StrategyPanel_t.
    /// </summary>
    public static readonly ElementDefinition StrategyPanel_t = new(
        AtdlNamespaces.lay + "StrategyPanel",
        typeof(StrategyPanel_t),
        [
            new ConstructorParameter(typeof(Strategy_t), SourceType.NamedPredecessor, "CurrentStrategy"),
            new ConstructorParameter(typeof(IStrategyPanel), SourceType.ParentObject, string.Empty)
        ],
        StrategyPanelAttributes,
        [
            new ChildElementDefinition(new RecursiveTypeElementDefinition(), "StrategyPanels",
                typeof(StrategyPanelCollection), StandardContainerMethod.Add),
            new ChildElementDefinition(Control_t, "Controls",
                typeof(ControlCollection), StandardContainerMethod.Add)
        ]);

    #endregion // StrategyPanel_t Definition

    #region StrategyLayout_t Definition

    /// <summary>
    /// Defines the content of StrategyLayout_t.
    /// </summary>
    public static readonly ElementDefinition StrategyLayout_t = new(
        AtdlNamespaces.lay + "StrategyLayout", typeof(StrategyLayout_t),
        [], new ChildElementDefinition(
            StrategyPanel_t, "StrategyPanel", typeof(StrategyPanel_t), StandardContainerMethod.Assign));

    #endregion

    #region StrategyEdit_t Definition

    private static readonly ElementAttribute[] StrategyEditAttributes =
    [
        new("errorMessage", "ErrorMessage", typeof(string), Required.Mandatory)
    ];

    /// <summary>
    /// Defines the content of StrategyEdit_t.
    /// </summary>
    public static readonly ElementDefinition StrategyEdit_t = new(
        AtdlNamespaces.val + "StrategyEdit", typeof(StrategyEdit_t),
        StrategyEditAttributes,
        [
            new ChildElementDefinition(Edit_t_IParameter_t, "Edit",
                typeof(Edit_t<IParameter>), StandardContainerMethod.Assign),
            new ChildElementDefinition(EditRef_t_IParameter_t, "EditRef",
                typeof(EditRef_t<IParameter>), StandardContainerMethod.Assign)
        ]);

    #endregion // StrategyEdit_t Definition

    #region Strategy_t Definition

    private static readonly ElementAttribute[] StrategyAttributes =
    [
        new("disclosureDoc", "DisclosureDoc", typeof(string), Required.Optional),
        new("fixMsgType", "FixMsgType", typeof(string), Required.Optional),
        new("imageLocation", "ImageLocation", typeof(string), Required.Optional),
        new("name", "Name", typeof(string), Required.Mandatory),
        new("orderSequenceTag", "OrderSequenceTag", typeof(FixTag), Required.Optional),
        new("providerID", "ProviderId", typeof(string), Required.Optional),
        new("providerSubID", "ProviderSubId", typeof(string), Required.Optional),
        new("sentOrderLink", "SentOrderLink", typeof(string), Required.Optional),
        new("totalLegs", "TotalLegs", typeof(NumInGroup), Required.Optional),
        new("totalOrders", "TotalOrders", typeof(NumInGroup), Required.Optional),
        new("uiRep", "UiRep", typeof(string), Required.Optional),
        new("version", "Version", typeof(string), Required.Mandatory),
        new("wireValue", "WireValue", typeof(string), Required.Mandatory)
    ];

    /// <summary>
    /// Defines the content of Strategy_t.
    /// </summary>
    public static readonly ElementDefinition Strategy_t = new(
        AtdlNamespaces.core + "Strategy", typeof(Strategy_t), StrategyAttributes,
        [
            new ChildElementDefinition(Parameter_t, "Parameters", typeof(ParameterCollection), StandardContainerMethod.Add),
            new ChildElementDefinition(Edit_t, "Edits", typeof(EditCollection), StandardContainerMethod.Add),
            new ChildElementDefinition(StrategyLayout_t, "StrategyLayout", typeof(StrategyLayout_t), StandardContainerMethod.Assign),
            new ChildElementDefinition(StrategyEdit_t, "StrategyEdits", typeof(StrategyEditCollection), StandardContainerMethod.Add),
            new ChildElementDefinition(Regions, "Regions", typeof(RegionCollection), StandardContainerMethod.Add),
            new ChildElementDefinition(Markets, "Markets", typeof(MarketCollection), StandardContainerMethod.Add),
            new ChildElementDefinition(SecurityTypes, "SecurityTypes", typeof(SecurityTypeCollection), StandardContainerMethod.Add)
        ],
        new CacheElementValueInstruction("CurrentStrategy"));

    // RepeatingGroup

    #endregion //Strategy_t Definition

    #region Strategies_t Definition

    private static readonly ElementAttribute[] StrategiesAttributes =
    [
        new("changeStrategyOnCxlRpl", "ChangeStrategyOnCxlRpl", typeof(bool), Required.Optional),
        new("draftFlagIdentifierTag", "DraftFlagIdentifierTag", typeof(FixTag), Required.Optional),
        new("imageLocation", "ImageLocation", typeof(string), Required.Optional),
        new("strategyIdentifierTag", "StrategyIdentifierTag", typeof(FixTag), Required.Mandatory),
        new("versionIdentifierTag", "VersionIdentifierTag", typeof(FixTag), Required.Optional),
        new("tag957Support", "Tag957Support", typeof(bool), Required.Optional),
    ];

    /// <summary>
    /// Defines the content of Strategies_t.
    /// </summary>
    public static readonly ElementDefinition Strategies_t = new(
        AtdlNamespaces.core + "Strategies", typeof(Strategies_t), StrategiesAttributes,
        [
            new ChildElementDefinition(Strategy_t, "Strategies", typeof(StrategyCollection), StandardContainerMethod.Add),
            new ChildElementDefinition(Edit_t, "Edits", typeof(EditCollection), StandardContainerMethod.Add)
        ]);

    #endregion // Strategies_t Definition
}

