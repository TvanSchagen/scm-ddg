MATCH (o:Organization)-[:PERFORMS]->(a:Activity)-[:PRODUCES]->(p:Product),
(a)-[LOCATED_AT]->(l:Location)
WHERE l.latitude > -40.0
AND l.latitude < 44.0
AND l.longtitude > -30.0
AND l.longtitude < 70.0
RETURN o.name, o.email_address, l.country, l.city, p.name, a.name
