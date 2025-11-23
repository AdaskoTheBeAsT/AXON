package axon;

import java.util.List;

public record Schema(String name, List<FieldDefinition> fields) {
}
