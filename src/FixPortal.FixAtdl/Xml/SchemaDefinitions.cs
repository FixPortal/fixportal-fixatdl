// FP Enhancement: 2026-05-24 — modernised for net10 (file-scoped, nullable, FixPortal namespace).
#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

using FixPortal.FixAtdl.Fix;
using FixPortal.FixAtdl.Model.Reference;
using FixPortal.FixAtdl.Model.Types;
using FixPortal.FixAtdl.Model.Types.Support;
using FixPortal.FixAtdl.Xml.Serialization;

namespace FixPortal.FixAtdl.Xml;

/// <summary>
/// Provides the definition of the FIXatdl schema.
/// </summary>
public static class SchemaDefinitions
{
    #region SecurityType_t Definition

    private static readonly ElementAttribute[] SecurityTypeAttributes =
    [
        new ElementAttribute("name", "Name", typeof(string), Required.Mandatory),
        new ElementAttribute("inclusion", "Inclusion", EnumDefinitions.Inclusion_t, Required.Mandatory)
    ];

    private static readonly ElementDefinition SecurityType_t = new(
        AtdlNamespaces.core + "SecurityType", typeof(Model.Elements.SecurityType_t), SecurityTypeAttributes);

    private static readonly ContainerElementDefinition SecurityTypes = new(
        AtdlNamespaces.core + "SecurityTypes", SecurityType_t);

    #endregion // SecurityType_t Definition

    #region Market_t Definition

    private static readonly ElementAttribute[] MarketAttributes =
    [
        new ElementAttribute("MICCode", "MICCode", typeof(string), Required.Mandatory),
        new ElementAttribute("inclusion", "Inclusion", EnumDefinitions.Inclusion_t, Required.Mandatory)
    ];

    private static readonly ElementDefinition Market_t = new(
        AtdlNamespaces.core + "Market", typeof(Model.Elements.Market_t), MarketAttributes);

    private static readonly ContainerElementDefinition Markets = new(
        AtdlNamespaces.core + "Markets", Market_t);

    #endregion // Market_t Definition

    #region Region_t & Country_t Definition

    private static readonly ElementAttribute[] CountryAttributes =
    [
        new ElementAttribute("CountryCode", "CountryCode", typeof(IsoCountryCode), Required.Mandatory),
        new ElementAttribute("inclusion", "Inclusion", EnumDefinitions.Inclusion_t, Required.Mandatory)
    ];

    private static readonly ElementDefinition Country_t = new(
        AtdlNamespaces.core + "Country", typeof(FixPortal.FixAtdl.Model.Elements.Country_t), CountryAttributes);

    private static readonly ElementAttribute[] RegionAttributes =
    [
        new ElementAttribute("name", "Name", EnumDefinitions.Region, Required.Mandatory),
        new ElementAttribute("inclusion", "Inclusion", EnumDefinitions.Inclusion_t, Required.Mandatory)
    ];

    private static readonly ElementDefinition Region_t = new(
        AtdlNamespaces.core + "Region", typeof(Model.Elements.Region_t), RegionAttributes,
        new ChildElementDefinition(Country_t, "Countries",
                typeof(Model.Collections.CountryCollection), StandardContainerMethod.Add));

    private static readonly ContainerElementDefinition Regions = new(
        AtdlNamespaces.core + "Regions", Region_t);

    #endregion // Region_t & Country_t Definition

    #region Parameter_t Definition

    private static readonly ConstructorParameter[] ParameterConstructorParameters =
    [
        new ConstructorParameter(typeof(string), SourceType.ElementAttribute, "name")
    ];

    private static readonly ElementAttribute[] ParameterCommonAttributes =
    [
        new ElementAttribute("definedByFIX", "DefinedByFix", typeof(bool), Required.Optional),
        new ElementAttribute("fixTag", "FixTag", typeof(FixTag), Required.Optional),
        new ElementAttribute("mutableOnCxlRpl", "MutableOnCxlRpl", typeof(bool), Required.Optional),
        new ElementAttribute("revertOnCxlRpl", "RevertOnCxlRpl", typeof(bool), Required.Optional),
        new ElementAttribute("use", "Use", EnumDefinitions.Use_t, Required.Optional),
    ];

    private static readonly ElementAttribute[] BooleanDefinition =
    [
        new ElementAttribute("constValue", "Value.ConstValue", typeof(bool), Required.Optional),
        new ElementAttribute("falseWireValue", "Value.FalseWireValue", typeof(string), Required.Optional),
        new ElementAttribute("trueWireValue", "Value.TrueWireValue", typeof(string), Required.Optional)
    ];

    private static readonly ElementAttribute[] CharDefinition =
    [
        new ElementAttribute("constValue", "Value.ConstValue", typeof(char), Required.Optional),
    ];

    private static readonly ElementAttribute[] CountryDefinition =
    [
        new ElementAttribute("constValue", "Value.ConstValue", typeof(IsoCountryCode), Required.Optional),
        new ElementAttribute("maxLength", "Value.MaxLength", typeof(int), Required.Optional),
        new ElementAttribute("minLength", "Value.MinLength", typeof(int), Required.Optional),
    ];

    private static readonly ElementAttribute[] CurrencyDefinition =
    [
        new ElementAttribute("constValue", "Value.ConstValue", typeof(IsoCurrencyCode), Required.Optional),
    ];

    private static readonly ElementAttribute[] DataDefinition =
    [
        new ElementAttribute("constValue", "Value.ConstValue", typeof(char[]), Required.Optional),
        new ElementAttribute("maxLength", "Value.MaxLength", typeof(int), Required.Optional),
        new ElementAttribute("minLength", "Value.MinLength", typeof(int), Required.Optional),
    ];

    private static readonly ElementAttribute[] ExchangeDefinition =
    [
        new ElementAttribute("constValue", "Value.ConstValue", typeof(string), Required.Optional)
    ];

    /// <remarks>Used for Amt_t, Float_t, Price_t, PriceOffset_t, Qty_t.  (Percentage_t has an extra attribute.)</remarks>
    private static readonly ElementAttribute[] FloatDefinition =
    [
        new ElementAttribute("constValue", "Value.ConstValue", typeof(decimal), Required.Optional),
        new ElementAttribute("maxValue", "Value.MaxValue", typeof(decimal), Required.Optional),
        new ElementAttribute("minValue", "Value.MinValue", typeof(decimal), Required.Optional),
        new ElementAttribute("precision", "Value.Precision", typeof(int), Required.Optional)
    ];

    private static readonly ElementAttribute[] IntDefinition =
    [
        new ElementAttribute("constValue", "Value.ConstValue", typeof(int), Required.Optional),
        new ElementAttribute("maxValue", "Value.MaxValue", typeof(int), Required.Optional),
        new ElementAttribute("minValue", "Value.MinValue", typeof(int), Required.Optional)
    ];

    private static readonly ElementAttribute[] LanguageDefinition =
    [
        new ElementAttribute("constValue", "Value.ConstValue", typeof(IsoLanguageCode), Required.Optional)
    ];

    // Used for Length_t, NumInGroup_t, SeqNum_t, TagNum_t
    private static readonly ElementAttribute[] LengthDefinition =
    [
        new ElementAttribute("constValue", "Value.ConstValue", typeof(int), Required.Optional),
    ];

    private static readonly ElementAttribute[] LocalMktDateDefinition =
    [
        new ElementAttribute("constValue", "Value.ConstValue", typeof(DateTime), Required.Optional),
        new ElementAttribute("maxValue", "Value.MaxValue", typeof(DateTime), Required.Optional),
        new ElementAttribute("minValue", "Value.MinValue", typeof(DateTime), Required.Optional)
    ];

    private static readonly ElementAttribute[] MonthYearDefinition =
    [
        new ElementAttribute("constValue", "Value.ConstValue", typeof(MonthYear), Required.Optional),
        new ElementAttribute("maxValue", "Value.MaxValue", typeof(MonthYear), Required.Optional),
        new ElementAttribute("minValue", "Value.MinValue", typeof(MonthYear), Required.Optional)
    ];

    private static readonly ElementAttribute[] PercentageDefinition =
    [
        new ElementAttribute("constValue", "Value.ConstValue", typeof(decimal), Required.Optional),
        new ElementAttribute("maxValue", "Value.MaxValue", typeof(decimal), Required.Optional),
        new ElementAttribute("minValue", "Value.MinValue", typeof(decimal), Required.Optional),
        new ElementAttribute("precision", "Value.Precision", typeof(int), Required.Optional),
        new ElementAttribute("multiplyBy100", "Value.MultiplyBy100", typeof(bool), Required.Optional),
    ];

    // Used for MultipleCharValue_t, MultipleStringValue_t
    private static readonly ElementAttribute[] MultipleStringValueDefinition =
    [
        new ElementAttribute("constValue", "Value.ConstValue", typeof(string), Required.Optional),
        new ElementAttribute("maxLength", "Value.MaxLength", typeof(int), Required.Optional),
        new ElementAttribute("minLength", "Value.MinLength", typeof(int), Required.Optional),
        new ElementAttribute("invertOnWire", "Value.InvertOnWire", typeof(bool), Required.Optional),
    ];

    private static readonly ElementAttribute[] StringDefinition =
    [
        new ElementAttribute("constValue", "Value.ConstValue", typeof(string), Required.Optional),
        new ElementAttribute("maxLength", "Value.MaxLength", typeof(int), Required.Optional),
        new ElementAttribute("minLength", "Value.MinLength", typeof(int), Required.Optional),
    ];

    private static readonly ElementAttribute[] TenorDefinition =
    [
        new ElementAttribute("constValue", "Value.ConstValue", typeof(Tenor), Required.Optional)
    ];

    // Used for TZTimeOnly_t, TZTimestamp_t, UTCDateOnly_t, UTCTimeOnly_t
    // (UTCTimestamp_t has an extra attribute.)
    private static readonly ElementAttribute[] TZTimeOnlyDefinition =
    [
        new ElementAttribute("constValue", "Value.ConstValue", typeof(DateTime), Required.Optional),
        new ElementAttribute("maxValue", "Value.MaxValue", typeof(DateTime), Required.Optional),
        new ElementAttribute("minValue", "Value.MinValue", typeof(DateTime), Required.Optional)
    ];

    private static readonly ElementAttribute[] UTCTimestampDefinition =
    [
        new ElementAttribute("constValue", "Value.ConstValue", typeof(DateTime), Required.Optional),
        new ElementAttribute("maxValue", "Value.MaxValue", typeof(DateTime), Required.Optional),
        new ElementAttribute("minValue", "Value.MinValue", typeof(DateTime), Required.Optional),
        new ElementAttribute("localMktTz", "Value.LocalMktTz", typeof(string), Required.Optional)
    ];

    private static readonly ChildElementDefinition EnumPairs = new(
        new ElementDefinition(AtdlNamespaces.core + "EnumPair", typeof(Model.Elements.EnumPair_t),
            [
                new ElementAttribute("enumID", "EnumId", typeof(string), Required.Mandatory),
                new ElementAttribute("wireValue", "WireValue", typeof(string), Required.Mandatory)
            ]),
            "EnumPairs", typeof(Model.Collections.EnumPairCollection), StandardContainerMethod.Add);

    /// <summary>
    /// Defines the content of Parameter_t.
    /// </summary>
    public static readonly GenericTypeElementDefinition Parameter_t = new(
        AtdlNamespaces.core + "Parameter", typeof(Model.Elements.Parameter_t<>), AtdlNamespaces.xsi + "type", "FixPortal.FixAtdl.Model.Types",
        ParameterConstructorParameters, ParameterCommonAttributes,
        new Dictionary<Type, ElementAttribute[]>()
        {
                {  typeof(Amt_t), FloatDefinition },
                {  typeof(Boolean_t), BooleanDefinition },
                {  typeof(Char_t), CharDefinition },
                {  typeof(Country_t), CountryDefinition },
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
        new ElementAttribute("id", "Id", typeof(string), Required.Mandatory)
    ];

    /// <summary>
    /// Defines the content of EditRef_t when it relates to a control.
    /// </summary>
    public static readonly ElementDefinition EditRef_t_Control_t = new(
        AtdlNamespaces.val + "EditRef", typeof(Model.Elements.EditRef_t<Model.Elements.Control_t>), EditRefAttributes);

    /// <summary>
    /// Defines the content of EditRef_t when it relates to a parameter.
    /// </summary>
    public static readonly ElementDefinition EditRef_t_IParameter_t = new(
        AtdlNamespaces.val + "EditRef", typeof(Model.Elements.EditRef_t<Model.Elements.Support.IParameter>), EditRefAttributes);

    #endregion // EditRef_t<T> Definitions

    #region Edit_t Definitions

    private static readonly ElementAttribute[] EditAttributes =
    [
        new ElementAttribute("field", "Field", typeof(string), Required.Optional),
        new ElementAttribute("field2", "Field2", typeof(string), Required.Optional),
        new ElementAttribute("id", "Id", typeof(string), Required.Optional),
        new ElementAttribute("logicOperator", "LogicOperator", EnumDefinitions.LogicOperator_t, Required.Optional),
        new ElementAttribute("operator", "Operator", EnumDefinitions.Operator_t, Required.Optional),
        new ElementAttribute("value", "Value", typeof(string), Required.Optional)
    ];

    /// <summary>
    /// Defines the content of Edit_t.
    /// </summary>
    public static readonly ElementDefinition Edit_t = new(
        AtdlNamespaces.val + "Edit", typeof(Model.Elements.Edit_t), EditAttributes,
        [
            new ChildElementDefinition(new RecursiveTypeElementDefinition(), "Edits",
                typeof(Model.Collections.EditCollection), StandardContainerMethod.Add)
        ]);

    private static readonly ElementDefinition Edit_t_Control_t = new(
        AtdlNamespaces.val + "Edit", typeof(Model.Elements.Edit_t<Model.Elements.Control_t>), EditAttributes,
        [
            new ChildElementDefinition(new RecursiveTypeElementDefinition(), "Edits",
                typeof(Model.Collections.EditEvaluatingCollection<Model.Elements.Control_t>), StandardContainerMethod.Add),
            new ChildElementDefinition(EditRef_t_Control_t, "EditRefs",
                typeof(Model.Collections.EditRefCollection<Model.Elements.Control_t>), StandardContainerMethod.Add)
        ]);

    private static readonly ElementDefinition Edit_t_IParameter_t = new(
        AtdlNamespaces.val + "Edit", typeof(Model.Elements.Edit_t<Model.Elements.Support.IParameter>), EditAttributes,
        [
            new ChildElementDefinition(new RecursiveTypeElementDefinition(), "Edits",
                typeof(Model.Collections.EditEvaluatingCollection<Model.Elements.Support.IParameter>), StandardContainerMethod.Add),
            new ChildElementDefinition(EditRef_t_IParameter_t, "EditRefs",
                typeof(Model.Collections.EditRefCollection<Model.Elements.Support.IParameter>), StandardContainerMethod.Add)
        ]);

    #endregion // Edit_t Definitions

    #region StateRule_t Definition

    private static readonly ElementAttribute[] StateRuleAttibutes =
    [
        new ElementAttribute("enabled", "Enabled", typeof(bool), Required.Optional),
        new ElementAttribute("visible", "Visible", typeof(bool), Required.Optional),
        new ElementAttribute("value", "Value", typeof(string), Required.Optional)
    ];

    /// <summary>
    /// Defines the content of StateRule_t.
    /// </summary>
    public static readonly ElementDefinition StateRule_t = new(
        AtdlNamespaces.flow + "StateRule", typeof(Model.Elements.StateRule_t), StateRuleAttibutes,
            [
                new ChildElementDefinition(Edit_t_Control_t, "Edit", typeof(Model.Elements.Edit_t<Model.Elements.Control_t>), StandardContainerMethod.Assign),
                new ChildElementDefinition(EditRef_t_Control_t, "EditRef", typeof(Model.Elements.EditRef_t<Model.Elements.Control_t>), StandardContainerMethod.Assign)
            ]);

    #endregion // StateRule_t Definition

    #region ListItem_t Definition

    private static readonly ElementAttribute[] ListItemAttibutes =
    [
        new ElementAttribute("uiRep", "UiRep", typeof(string), Required.Mandatory),
        new ElementAttribute("enumID", "EnumId", typeof(string), Required.Mandatory)
    ];

    /// <summary>
    /// Defines the content of ListItem_t.
    /// </summary>
    public static readonly ElementDefinition ListItem_t = new(
        AtdlNamespaces.lay + "ListItem", typeof(Model.Elements.ListItem_t), ListItemAttibutes);

    #endregion // ListItem_t Definition

    #region Control_t Definition

    private static readonly ElementAttribute[] ControlCommonAttributes =
    [
        new ElementAttribute("disableForTemplate","DisableForTemplate", typeof(bool), Required.Optional),
        new ElementAttribute("initFixField","InitFixField", typeof(string), Required.Optional),
        new ElementAttribute("initPolicy","InitPolicy", EnumDefinitions.InitPolicy_t, Required.Optional),
        new ElementAttribute("label", "Label", typeof(string), Required.Optional),
        new ElementAttribute("parameterRef","ParameterRef", typeof(string), Required.Optional),
        new ElementAttribute("tooltip","ToolTip", typeof(string), Required.Optional)
    ];

    private static readonly ElementAttribute[] CheckBoxAttributes =
    [
        new ElementAttribute("checkedEnumRef", "CheckedEnumRef", typeof(string), Required.Optional),
        new ElementAttribute("uncheckedEnumRef", "UncheckedEnumRef", typeof(string), Required.Optional),
        new ElementAttribute("initValue", "InitValue", typeof(bool), Required.Optional)
    ];

    private static readonly ElementAttribute[] CheckBoxListAttributes =
    [
        new ElementAttribute("initValue", "InitValue", typeof(string), Required.Optional),
        new ElementAttribute("orientation", "Orientation", EnumDefinitions.Orientation_t, Required.Optional)
    ];

    private static readonly ElementAttribute[] ClockAttributes =
    [
        new ElementAttribute("initValue", "InitValue", typeof(Model.Controls.InitValueClock), Required.Optional),
        new ElementAttribute("initValueMode", "InitValueMode", typeof(int), Required.Optional),
        new ElementAttribute("localMktTz", "LocalMktTz", typeof(string), Required.Optional)
    ];

    private static readonly ElementAttribute[] DoubleSpinnerAttributes =
    [
        new ElementAttribute("initValue", "InitValue", typeof(decimal), Required.Optional),
        new ElementAttribute("innerIncrement", "InnerIncrement", typeof(decimal), Required.Optional),
        new ElementAttribute("innerIncrementPolicy", "InnerIncrementPolicy", EnumDefinitions.IncrementPolicy_t, Required.Optional),
        new ElementAttribute("outerIncrement", "OuterIncrement", typeof(decimal), Required.Optional),
        new ElementAttribute("outerIncrementPolicy", "OuterIncrementPolicy", EnumDefinitions.IncrementPolicy_t, Required.Optional)
    ];

    private static readonly ElementAttribute[] DropDownListAttributes =
    [
        new ElementAttribute("initValue", "InitValue", typeof(string), Required.Optional)
    ];

    private static readonly ElementAttribute[] EditableDropDownListAttributes =
    [
        new ElementAttribute("initValue", "InitValue", typeof(string), Required.Optional)
    ];

    private static readonly ElementAttribute[] HiddenFieldAttributes =
    [
        new ElementAttribute("initValue", "InitValue", typeof(object), Required.Optional)
    ];

    private static readonly ElementAttribute[] LabelAttributes =
    [
        new ElementAttribute("initValue", "InitValue", typeof(string), Required.Optional)
    ];

    private static readonly ElementAttribute[] MultiSelectListAttributes =
    [
        new ElementAttribute("initValue", "InitValue", typeof(string), Required.Optional)
    ];

    private static readonly ElementAttribute[] RadioButtonAttributes =
    [
        new ElementAttribute("initValue", "InitValue", typeof(bool), Required.Optional),
        new ElementAttribute("checkedEnumRef", "CheckedEnumRef", typeof(string), Required.Optional),
        new ElementAttribute("uncheckedEnumRef", "UncheckedEnumRef", typeof(string), Required.Optional),
        new ElementAttribute("radioGroup", "RadioGroup", typeof(string), Required.Optional)
    ];

    private static readonly ElementAttribute[] RadioButtonListAttributes =
    [
        new ElementAttribute("initValue", "InitValue", typeof(string), Required.Optional),
        new ElementAttribute("orientation", "Orientation", EnumDefinitions.Orientation_t, Required.Optional)
    ];

    private static readonly ElementAttribute[] SingleSelectListAttributes =
    [
        new ElementAttribute("initValue", "InitValue", typeof(string), Required.Optional)
    ];

    private static readonly ElementAttribute[] SingleSpinnerAttributes =
    [
        new ElementAttribute("initValue", "InitValue", typeof(decimal), Required.Optional),
        new ElementAttribute("increment", "Increment", typeof(decimal), Required.Optional),
        new ElementAttribute("incrementPolicy", "IncrementPolicy", EnumDefinitions.IncrementPolicy_t, Required.Optional)
    ];

    private static readonly ElementAttribute[] SliderAttributes =
    [
        new ElementAttribute("initValue", "InitValue", typeof(string), Required.Optional)
    ];

    private static readonly ElementAttribute[] TextFieldAttributes =
    [
        new ElementAttribute("initValue", "InitValue", typeof(string), Required.Optional)
    ];

    /// <summary>
    /// Defines the content of Control_t.
    /// </summary>
    public static readonly MultiTypeElementDefinition Control_t = new(
        AtdlNamespaces.lay + "Control", AtdlNamespaces.xsi + "type", "FixPortal.FixAtdl.Model.Controls",
        [new ConstructorParameter(typeof(string), SourceType.ElementAttribute, "ID")],
        ControlCommonAttributes,
        new Dictionary<Type, ElementAttribute[]>()
        {
            { typeof(Model.Controls.CheckBox_t), CheckBoxAttributes },
            { typeof(Model.Controls.CheckBoxList_t), CheckBoxListAttributes },
            { typeof(Model.Controls.Clock_t), ClockAttributes },
            { typeof(Model.Controls.DoubleSpinner_t), DoubleSpinnerAttributes },
            { typeof(Model.Controls.DropDownList_t), DropDownListAttributes },
            { typeof(Model.Controls.EditableDropDownList_t), EditableDropDownListAttributes },
            { typeof(Model.Controls.HiddenField_t), HiddenFieldAttributes },
            { typeof(Model.Controls.Label_t), LabelAttributes },
            { typeof(Model.Controls.MultiSelectList_t), MultiSelectListAttributes },
            { typeof(Model.Controls.RadioButton_t), RadioButtonAttributes },
            { typeof(Model.Controls.RadioButtonList_t), RadioButtonListAttributes },
            { typeof(Model.Controls.SingleSelectList_t), SingleSelectListAttributes },
            { typeof(Model.Controls.SingleSpinner_t), SingleSpinnerAttributes },
            { typeof(Model.Controls.Slider_t), SliderAttributes },
            { typeof(Model.Controls.TextField_t), TextFieldAttributes }
        },
        [
            new ChildElementDefinition(ListItem_t, "ListItems", typeof(Model.Collections.ListItemCollection), StandardContainerMethod.Add),
            new ChildElementDefinition(StateRule_t, "StateRules", typeof(Model.Collections.StateRuleCollection), StandardContainerMethod.Add)
        ]);

    #endregion // Control_t Definition

    #region StrategyPanel_t Definition

    private static readonly ElementAttribute[] StrategyPanelAttributes =
    [
        new ElementAttribute("border", "Border", EnumDefinitions.Border_t, Required.Optional),
        new ElementAttribute("collapsed", "Collapsed", typeof(bool), Required.Optional),
        new ElementAttribute("collapsible", "Collapsible", typeof(bool), Required.Optional),
        new ElementAttribute("color", "Color", typeof(string), Required.Optional),
        new ElementAttribute("orientation", "Orientation", EnumDefinitions.Orientation_t, Required.Optional),
        new ElementAttribute("title", "Title", typeof(string), Required.Optional)
    ];

    /// <summary>
    /// Defines the content of StrategyPanel_t.
    /// </summary>
    public static readonly ElementDefinition StrategyPanel_t = new(
        AtdlNamespaces.lay + "StrategyPanel",
        typeof(Model.Elements.StrategyPanel_t),
        [
            new ConstructorParameter(typeof(Model.Elements.Strategy_t), SourceType.NamedPredecessor, "CurrentStrategy"),
            new ConstructorParameter(typeof(Model.Elements.Support.IStrategyPanel), SourceType.ParentObject, string.Empty)
        ],
        StrategyPanelAttributes,
        [
            new ChildElementDefinition(new RecursiveTypeElementDefinition(), "StrategyPanels",
                typeof(Model.Collections.StrategyPanelCollection), StandardContainerMethod.Add),
            new ChildElementDefinition(Control_t, "Controls",
                typeof(Model.Collections.ControlCollection), StandardContainerMethod.Add)
        ]);

    #endregion // StrategyPanel_t Definition

    #region StrategyLayout_t Definition

    /// <summary>
    /// Defines the content of StrategyLayout_t.
    /// </summary>
    public static readonly ElementDefinition StrategyLayout_t = new(
        AtdlNamespaces.lay + "StrategyLayout", typeof(Model.Elements.StrategyLayout_t),
        [], new ChildElementDefinition(
            StrategyPanel_t, "StrategyPanel", typeof(Model.Elements.StrategyPanel_t), StandardContainerMethod.Assign));

    #endregion

    #region StrategyEdit_t Definition

    private static readonly ElementAttribute[] StrategyEditAttributes =
    [
        new ElementAttribute("errorMessage", "ErrorMessage", typeof(string), Required.Mandatory)
    ];

    /// <summary>
    /// Defines the content of StrategyEdit_t.
    /// </summary>
    public static readonly ElementDefinition StrategyEdit_t = new(
        AtdlNamespaces.val + "StrategyEdit", typeof(Model.Elements.StrategyEdit_t),
        StrategyEditAttributes,
        [
            new ChildElementDefinition(Edit_t_IParameter_t, "Edit",
                typeof(Model.Elements.Edit_t<Model.Elements.Support.IParameter>), StandardContainerMethod.Assign),
            new ChildElementDefinition(EditRef_t_IParameter_t, "EditRef",
                typeof(Model.Elements.EditRef_t<Model.Elements.Support.IParameter>), StandardContainerMethod.Assign)
        ]);

    #endregion // StrategyEdit_t Definition

    #region Strategy_t Definition

    private static readonly ElementAttribute[] StrategyAttributes =
    [
        new ElementAttribute("disclosureDoc", "DisclosureDoc", typeof(string), Required.Optional),
        new ElementAttribute("fixMsgType", "FixMsgType", typeof(string), Required.Optional),
        new ElementAttribute("imageLocation", "ImageLocation", typeof(string), Required.Optional),
        new ElementAttribute("name", "Name", typeof(string), Required.Mandatory),
        new ElementAttribute("orderSequenceTag", "OrderSequenceTag", typeof(FixTag), Required.Optional),
        new ElementAttribute("providerID", "ProviderId", typeof(string), Required.Optional),
        new ElementAttribute("providerSubID", "ProviderSubId", typeof(string), Required.Optional),
        new ElementAttribute("sentOrderLink", "SentOrderLink", typeof(string), Required.Optional),
        new ElementAttribute("totalLegs", "TotalLegs", typeof(NumInGroup), Required.Optional),
        new ElementAttribute("totalOrders", "TotalOrders", typeof(NumInGroup), Required.Optional),
        new ElementAttribute("uiRep", "UiRep", typeof(string), Required.Optional),
        new ElementAttribute("version", "Version", typeof(string), Required.Mandatory),
        new ElementAttribute("wireValue", "WireValue", typeof(string), Required.Mandatory)
    ];

    /// <summary>
    /// Defines the content of Strategy_t.
    /// </summary>
    public static readonly ElementDefinition Strategy_t = new(
        AtdlNamespaces.core + "Strategy", typeof(Model.Elements.Strategy_t), StrategyAttributes,
        [
            new ChildElementDefinition(Parameter_t, "Parameters", typeof(Model.Collections.ParameterCollection), StandardContainerMethod.Add),
            new ChildElementDefinition(Edit_t, "Edits", typeof(Model.Collections.EditCollection), StandardContainerMethod.Add),
            new ChildElementDefinition(StrategyLayout_t, "StrategyLayout", typeof(Model.Elements.StrategyLayout_t), StandardContainerMethod.Assign),
            new ChildElementDefinition(StrategyEdit_t, "StrategyEdits", typeof(Model.Collections.StrategyEditCollection), StandardContainerMethod.Add),
            new ChildElementDefinition(Regions, "Regions", typeof(Model.Collections.RegionCollection), StandardContainerMethod.Add),
            new ChildElementDefinition(Markets, "Markets", typeof(Model.Collections.MarketCollection), StandardContainerMethod.Add),
            new ChildElementDefinition(SecurityTypes, "SecurityTypes", typeof(Model.Collections.SecurityTypeCollection), StandardContainerMethod.Add)
        ],
        new CacheElementValueInstruction("CurrentStrategy"));

    // RepeatingGroup

    #endregion //Strategy_t Definition

    #region Strategies_t Definition

    private static readonly ElementAttribute[] StrategiesAttributes =
    [
        new ElementAttribute("changeStrategyOnCxlRpl", "ChangeStrategyOnCxlRpl", typeof(bool), Required.Optional),
        new ElementAttribute("draftFlagIdentifierTag", "DraftFlagIdentifierTag", typeof(FixTag), Required.Optional),
        new ElementAttribute("imageLocation", "ImageLocation", typeof(string), Required.Optional),
        new ElementAttribute("strategyIdentifierTag", "StrategyIdentifierTag", typeof(FixTag), Required.Mandatory),
        new ElementAttribute("versionIdentifierTag", "VersionIdentifierTag", typeof(FixTag), Required.Optional),
        new ElementAttribute("tag957Support", "Tag957Support", typeof(bool), Required.Optional),
    ];

    /// <summary>
    /// Defines the content of Strategies_t.
    /// </summary>
    public static readonly ElementDefinition Strategies_t = new(
        AtdlNamespaces.core + "Strategies", typeof(Model.Elements.Strategies_t), StrategiesAttributes,
        [
            new ChildElementDefinition(Strategy_t, "Strategies", typeof(Model.Collections.StrategyCollection), StandardContainerMethod.Add),
            new ChildElementDefinition(Edit_t, "Edits", typeof(Model.Collections.EditCollection), StandardContainerMethod.Add)
        ]);

    #endregion // Strategies_t Definition
}

