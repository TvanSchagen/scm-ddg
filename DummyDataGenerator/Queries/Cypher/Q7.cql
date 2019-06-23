MATCH (o:Organization)-[:PERFORMS]->(a:Activity)-[:PRODUCES]->(p:Product)
WHERE p.description CONTAINS 'corporis dolor'
RETURN ID(p), p.name, p.ean, p.description, o.name, o.description