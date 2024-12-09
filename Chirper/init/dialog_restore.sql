SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

\connect postgres

CREATE TABLE IF NOT EXISTS public."DialogMessage"
(
    "from" uuid NOT NULL,
    "to" uuid NOT NULL,
    text text COLLATE pg_catalog."default" NOT NULL,
    datesend timestamp with time zone DEFAULT now()
);

CREATE INDEX ON public."DialogMessage" ("from");
CREATE INDEX ON public."DialogMessage" ("to");
CREATE INDEX ON public."DialogMessage" ("datesend");

ALTER TABLE IF EXISTS public."DialogMessage"
    OWNER to postgres;

DO $$
DECLARE
    worker_count INT;
BEGIN
    LOOP
        SELECT COUNT(*) INTO worker_count FROM citus_get_active_worker_nodes();

        IF worker_count > 0 THEN
            EXIT;  
        END IF;

        PERFORM pg_sleep(1); 
    END LOOP;
    RAISE NOTICE 'Найдены рабочие узлы: %', worker_count;
END $$;

SELECT create_distributed_table('public."DialogMessage"', 'from');

COPY public."DialogMessage" ("from", "to", text, datesend) FROM stdin;
\.
COPY public."DialogMessage" ("from", "to", text, datesend) FROM '/var/lib/postgresql/restore/3393.dat';


CREATE FUNCTION public.list_message(
	from_uid uuid,
	to_uid uuid,
	count_skip integer,
	take integer)
    RETURNS TABLE(id uuid, friend_id uuid, msg_text text, datesend timestamp with time zone) 
    LANGUAGE 'plpgsql'
    COST 100
    VOLATILE PARALLEL UNSAFE
    ROWS 1000

AS $BODY$
begin
	return query SELECT d.from, d.to, d.text, d.datesend
	FROM "DialogMessage" d
	where d.from = from_uid and d.to = to_uid
	ORDER BY datesend DESC
	OFFSET (@count_skip) ROWS FETCH NEXT (@Take) ROWS ONLY;
end;
$BODY$;

ALTER FUNCTION public.list_message(uuid, uuid, integer, integer)
    OWNER TO postgres;


CREATE FUNCTION public.send_message(
	from_uid uuid,
	to_uid uuid,
	msg character varying)
    RETURNS boolean
    LANGUAGE 'plpgsql'
    COST 100
    VOLATILE PARALLEL UNSAFE
AS $BODY$
declare cnt integer;
begin
	INSERT INTO public."DialogMessage"(
	"from", "to", text)
	VALUES (from_uid, to_uid, msg);
	
	return true;
end;
$BODY$;

ALTER FUNCTION public.send_message(uuid, uuid, character varying)
    OWNER TO postgres;

--- Shard utils

CREATE TABLE public.isolated_tenants (
    tenant_id UUID PRIMARY KEY,
    isolated_at TIMESTAMP DEFAULT NOW()
);

CREATE OR REPLACE VIEW public.chatty_tenants AS
WITH message_counts AS (
    SELECT 
        "DialogMessage"."from" AS tenant_id,
        COUNT(*) AS message_count
    FROM 
        "DialogMessage"
    WHERE 
        "DialogMessage".datesend >= (NOW() - '7 days'::interval)
    GROUP BY 
        "DialogMessage"."from"
), isolated AS (
    SELECT 
        isolated_tenants.tenant_id
    FROM 
        isolated_tenants
)
SELECT 
    mc.tenant_id, mc.message_count
FROM 
    message_counts mc
LEFT JOIN 
    isolated i ON mc.tenant_id = i.tenant_id
WHERE 
    i.tenant_id IS NULL
ORDER BY 
    mc.message_count DESC
LIMIT 10;

ALTER TABLE public.chatty_tenants
    OWNER TO postgres;

CREATE FUNCTION public.isolate_chatty_tenant(
    tenant_id UUID,
    dest_host TEXT,
    dest_port INT
) RETURNS VOID AS $$
DECLARE
    new_shard_id BIGINT;

BEGIN
    new_shard_id := isolate_tenant_to_new_shard('public."DialogMessage"', tenant_id, shard_transfer_mode => 'force_logical');

    WITH si AS (
        SELECT nodename, nodeport
        FROM pg_dist_placement AS placement
        JOIN pg_dist_node AS node ON placement.groupid = node.groupid
        WHERE node.noderole = 'primary'
          AND shardid = new_shard_id
    )

    SELECT citus_move_shard_placement(
        new_shard_id,
        si.nodename, si.nodeport,
        dest_host, dest_port,
        shard_transfer_mode => 'force_logical'
    )
    FROM si;  
    INSERT INTO public.isolated_tenants (tenant_id) VALUES (tenant_id);  

EXCEPTION
    WHEN OTHERS THEN
        RAISE NOTICE 'Ошибка: %', SQLERRM;
END;
$$ LANGUAGE plpgsql;

ALTER FUNCTION public.isolate_chatty_tenant(uuid, TEXT, int) OWNER TO postgres;
