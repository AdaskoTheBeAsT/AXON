package axon;

public enum AxonType {
    STRING('S'),
    INTEGER('I'),
    FLOAT('F'),
    BOOLEAN('B'),
    TIMESTAMP('T');
    
    private final char code;
    
    AxonType(char code) {
        this.code = code;
    }
    
    public char getCode() {
        return code;
    }
    
    public static AxonType fromCode(char code) {
        for (AxonType type : values()) {
            if (type.code == code) {
                return type;
            }
        }
        throw new IllegalArgumentException("Unknown type code: " + code);
    }
}
