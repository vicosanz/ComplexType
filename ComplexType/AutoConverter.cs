namespace ComplexType;

public class AutoConverter<FromT, ToT>(
    System.Converter<FromT, ToT> _convertToProviderExpression,
    System.Converter<ToT, FromT> _convertFromProviderExpression)
{
    private readonly System.Converter<FromT, ToT> convertToProviderExpression = _convertToProviderExpression;
    private readonly System.Converter<ToT, FromT> convertFromProviderExpression = _convertFromProviderExpression;

    public ToT Convert(FromT from) => convertToProviderExpression(from);
    public FromT Convert(ToT to) => convertFromProviderExpression(to);
}
