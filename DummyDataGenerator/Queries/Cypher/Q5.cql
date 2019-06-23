MATCH (po:Organization)-[:PERFORMS]->(a:Activity)-[:PRODUCES]->(p:Product)-[:CONSISTS_OF*]->(c:Product)<-[PRODUCES]-(ca:Activity)<-[:PERFORMS]-(co:Organization)
WHERE po.name = 'Top Level Organization Blanda - Bode'
RETURN p.ean, p.name, po.name, c.ean, c.name, co.name,
CASE 
	WHEN co.number_of_employees < 1000 THEN 'Small'
	WHEN co.number_of_employees > 5000 THEN 'Large'
	ELSE 'Regular' 
END AS BusinessType