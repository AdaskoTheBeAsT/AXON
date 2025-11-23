package axon;

import java.util.List;
import java.util.Map;

public record DataBlock(String schemaName, int count, List<Map<String, Object>> rows) {
}
