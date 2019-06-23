MATCH (p:Product)-[:CONSISTS_OF*]->(c:Product)
WHERE p.name = 'Top Level Product Awesome Steel Car'
RETURN p.name, ID(p), c.name, ID(c)