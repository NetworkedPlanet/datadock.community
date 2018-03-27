import { Enum } from '../../shared/enum';
export class TabsEnum extends Enum<string> {
    public static readonly BASIC = new Enum('BASIC');
    public static readonly DATATYPES = new Enum('DATATYPES');
    public static readonly PREDICATES = new Enum('PREDICATES');
    public static readonly PREVIEW = new Enum('PREVIEW');
}
