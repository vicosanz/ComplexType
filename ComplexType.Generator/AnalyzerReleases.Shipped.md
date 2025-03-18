## Release 1.0

### New Rules

Rule ID | Category        | Severity | Notes
--------|-----------------|----------|--------------------
CTI001  | ComplexType     | Warning  | Struct must be declared as 'readonly partial record struct'.
CTI002  | ComplexType     | Warning  | Validate method must be declared as 'public static {0} Validate'.
CTI003  | ComplexType     | Warning  | Converter method must be declared as 'public static AutoConverter<{0}, {1}> Converter'.