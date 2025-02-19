namespace Abacus.Manager
{
    public static class UMLTemplate
    {
        public const string Source = @"
 @startuml

title {{Title}}
'https://plantuml.com/smetana02
!pragma layout smetana

{{Types}}

{{Dependencies}}

@enduml";
    }
}