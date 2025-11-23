use axon_parser::{parse, AxonValue};

fn main() {
    let axon_data = r#"
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
"#;

    match parse(axon_data) {
        Ok(result) => {
            println!(
                "Parsed {} schema(s) and {} data block(s)\n",
                result.schemas.len(),
                result.data_blocks.len()
            );

            for schema in &result.schemas {
                println!("Schema: {}", schema.name);
                for field in &schema.fields {
                    let nullable = if field.is_nullable { "?" } else { "" };
                    println!("  - {}: {:?}{}", field.name, field.field_type, nullable);
                }
                println!();
            }

            for data_block in &result.data_blocks {
                println!("Data: {} ({} rows)", data_block.schema_name, data_block.rows.len());
                for row in &data_block.rows {
                    print!("  ");
                    for (key, value) in row {
                        let val_str = match value {
                            AxonValue::Null => "null".to_string(),
                            AxonValue::String(s) => s.clone(),
                            AxonValue::Integer(i) => i.to_string(),
                            AxonValue::Float(f) => f.to_string(),
                            AxonValue::Boolean(b) => b.to_string(),
                            AxonValue::Timestamp(t) => t.clone(),
                        };
                        print!("{}={} ", key, val_str);
                    }
                    println!();
                }
                println!();
            }
        }
        Err(e) => eprintln!("Error parsing AXON: {}", e),
    }
}
