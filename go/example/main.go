package main

import (
	"fmt"
	"log"

	"github.com/axon/axon"
)

func main() {
	axonData := `
@schema User
id:I
name:S
email:S
active:B
age:I?
@end

@data User[3]
1|Alice|alice@example.com|1|28
2|Bob|bob@example.com|0|_
3|Carol|carol@example.com|1|35
@end
`

	result, err := axon.Parse(axonData)
	if err != nil {
		log.Fatal(err)
	}

	fmt.Printf("Parsed %d schema(s) and %d data block(s)\n\n",
		len(result.Schemas), len(result.DataBlocks))

	for _, schema := range result.Schemas {
		fmt.Printf("Schema: %s\n", schema.Name)
		for _, field := range schema.Fields {
			nullable := ""
			if field.IsNullable {
				nullable = "?"
			}
			fmt.Printf("  - %s: %s%s\n", field.Name, field.Type, nullable)
		}
		fmt.Println()
	}

	for _, dataBlock := range result.DataBlocks {
		fmt.Printf("Data: %s (%d rows)\n", dataBlock.SchemaName, len(dataBlock.Rows))
		for _, row := range dataBlock.Rows {
			fmt.Print("  ")
			for key, value := range row {
				fmt.Printf("%s=%v ", key, value)
			}
			fmt.Println()
		}
		fmt.Println()
	}
}
