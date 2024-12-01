--
-- NOTE:
--
-- File paths need to be edited. Search for $$PATH$$ and
-- replace it with the path to the directory containing
-- the extracted data files.
--
--
-- PostgreSQL database dump
--

-- Dumped from database version 15.6 (Debian 15.6-1.pgdg120+2)
-- Dumped by pg_dump version 16.0

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

DROP DATABASE IF EXISTS gigglebook;
--
-- Name: gigglebook; Type: DATABASE; Schema: -; Owner: postgres
--

CREATE DATABASE gigglebook WITH TEMPLATE = template0 ENCODING = 'UTF8' ICU_LOCALE = 'ru-RU' LOCALE_PROVIDER = 'icu';


ALTER DATABASE gigglebook OWNER TO postgres;

\connect gigglebook

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

--
-- Name: gender; Type: DOMAIN; Schema: public; Owner: postgres
--

CREATE DOMAIN public.gender AS character(1)
	CONSTRAINT gender_check CHECK ((VALUE = ANY (ARRAY['F'::bpchar, 'M'::bpchar])));


ALTER DOMAIN public.gender OWNER TO postgres;

--
-- Name: DialogMessage; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."DialogMessage" (
    "from" uuid NOT NULL,
    "to" uuid NOT NULL,
    text text NOT NULL,
    datesend timestamp with time zone DEFAULT now()
);


ALTER TABLE public."DialogMessage" OWNER TO postgres;

--
-- Name: Friends; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."Friends" (
    user_id uuid NOT NULL,
    friend_id uuid NOT NULL
);


ALTER TABLE public."Friends" OWNER TO postgres;

--
-- Name: Post; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."Post" (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    text text NOT NULL,
    author_user_id uuid NOT NULL,
	created_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
	updated_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP
);


ALTER TABLE public."Post" OWNER TO postgres;

--
-- Name: User; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."User" (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    first_name character varying(100) NOT NULL,
    second_name character varying(100) NOT NULL,
    birthdate date NOT NULL,
    biography character varying,
    city character varying NOT NULL,
    password character varying NOT NULL,
    username character varying,
    sex public.gender,
	created_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP
);


ALTER TABLE public."User" OWNER TO postgres;
--
-- Data for Name: User; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public."User" (id, first_name, second_name, birthdate, biography, city, password, username, sex, created_at) FROM stdin;
\.
COPY public."User" (id, first_name, second_name, birthdate, biography, city, password, username, sex, created_at) FROM '/var/lib/postgresql/restore/3395.dat';

--
-- Data for Name: DialogMessage; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public."DialogMessage" ("from", "to", text, datesend) FROM stdin;
\.
COPY public."DialogMessage" ("from", "to", text, datesend) FROM '/var/lib/postgresql/restore/3393.dat';

--
-- Data for Name: Friends; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public."Friends" (user_id, friend_id) FROM stdin;
\.
COPY public."Friends" (user_id, friend_id) FROM '/var/lib/postgresql/restore/3394.dat';

--
-- Data for Name: Post; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public."Post" (id, text, author_user_id, created_at, updated_at) FROM stdin;
\.
COPY public."Post" (id, text, author_user_id, created_at, updated_at) FROM '/var/lib/postgresql/restore/3371.dat';


--
-- Name: User pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."User"
    ADD CONSTRAINT pkey PRIMARY KEY (id);


--
-- Name: Post post_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Post"
    ADD CONSTRAINT post_pkey PRIMARY KEY (id);


--
-- Name: Friends unique_friends; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Friends"
    ADD CONSTRAINT unique_friends UNIQUE (user_id, friend_id);


--
-- Name: User username_loginname; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."User"
    ADD CONSTRAINT username_loginname UNIQUE (username);


--
-- Name: DialogMessage dialog_from_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."DialogMessage"
    ADD CONSTRAINT dialog_from_user_id_fkey FOREIGN KEY ("from") REFERENCES public."User"(id) ON DELETE CASCADE;


--
-- Name: DialogMessage dialog_to_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."DialogMessage"
    ADD CONSTRAINT dialog_to_user_id_fkey FOREIGN KEY ("to") REFERENCES public."User"(id);


--
-- Name: Friends friend_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Friends"
    ADD CONSTRAINT friend_id_fkey FOREIGN KEY (friend_id) REFERENCES public."User"(id);


--
-- Name: Post post_author_uid; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Post"
    ADD CONSTRAINT post_author_uid FOREIGN KEY (author_user_id) REFERENCES public."User"(id) ON DELETE CASCADE;


--
-- PostgreSQL database dump complete
--

CREATE INDEX IF NOT EXISTS idx_user_created_at 
	ON public."User" (created_at);

CREATE INDEX IF NOT EXISTS idx_tsvector_username
	ON public."User"
	USING gin  (to_tsvector('russian'::regconfig, first_name::text),
				to_tsvector('russian'::regconfig, second_name::text));

CREATE INDEX IF NOT EXISTS idx_exact_username
	ON public."User"(first_name, second_name);

CREATE INDEX IF NOT EXISTS idx_post_created_at
    ON public."Post" USING btree
    (created_at ASC NULLS LAST)
    TABLESPACE pg_default;

CREATE INDEX IF NOT EXISTS idx_author_user_id
    ON public."Post" USING btree
    (author_user_id ASC NULLS LAST)
    TABLESPACE pg_default;

CREATE INDEX IF NOT EXISTS idx_friends_friend_id
    ON public."Friends" USING btree
    (friend_id ASC NULLS LAST)
    TABLESPACE pg_default;

CREATE INDEX IF NOT EXISTS idx_friends_user_id
    ON public."Friends" USING btree
    (user_id ASC NULLS LAST)
    TABLESPACE pg_default;

--
-- Name: add_friend(uuid, uuid); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.add_friend(current_user_id uuid, friend_user_id uuid) RETURNS boolean
    LANGUAGE plpgsql
    AS $$

declare cnt integer;
begin
	select count(*) into cnt from "Friends" f
	where f.user_id = current_user_id and f.friend_id = friend_user_id;
	
	if (cnt) > 0 then 
		raise exception 'Уже в друзьях';
	end if;
	
	INSERT INTO public."Friends"(
	user_id, friend_id)
	VALUES (current_user_id, friend_user_id);
	
	return true;
end;
$$;


ALTER FUNCTION public.add_friend(current_user_id uuid, friend_user_id uuid) OWNER TO postgres;

--
-- Name: auth_user(character, character); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.auth_user(user_name character, hash character) RETURNS TABLE(id uuid, username character varying, first_name character varying, second_name character varying, birthdate date, biography character varying, city character varying, sex public.gender)
    LANGUAGE plpgsql
    AS $$
begin
	return query select u.id, u.username, u.first_name, u.second_name, u.birthdate, u.biography, u.city, u.sex from "User" u
	where u.username = user_name and u.password = hash;
end;
$$;


ALTER FUNCTION public.auth_user(user_name character, hash character) OWNER TO postgres;

--
-- Name: find_user(character varying, character varying); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.find_user(
	user_fname character varying,
	user_sname character varying)
    RETURNS TABLE(
		id uuid, 
		username character varying, 
		first_name character varying, 
		second_name character varying, 
		birthdate date, 
		biography character varying, 
		city character varying, 
		sex public.gender) 
    LANGUAGE 'plpgsql'
    COST 100
    VOLATILE PARALLEL UNSAFE
    ROWS 1000

AS $$
begin
	return query 
	select u.id, u.username, u.first_name, u.second_name, u.birthdate, u.biography, u.city, u.sex from "User" u
		where 	to_tsvector('russian', u.first_name) @@ to_tsquery(user_fname || ':*'::varchar) 
			AND to_tsvector('russian', u.second_name) @@ to_tsquery(user_sname || ':*'::varchar) OR
			(u.first_name = user_fname AND u.second_name = user_sname) OR
        	(u.first_name = user_fname AND to_tsvector('russian', u.second_name) @@ to_tsquery(user_sname || ':*'::varchar)) OR
        	(to_tsvector('russian', u.first_name) @@ to_tsquery(user_fname || ':*'::varchar) AND u.second_name = user_sname);
END;
$$;


ALTER FUNCTION public.find_user(user_fname character varying, user_sname character varying) OWNER TO postgres;

--
-- Name: get_user(uuid); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.get_user(user_id uuid) RETURNS TABLE(id uuid, username character varying, first_name character varying, second_name character varying, birthdate date, biography character varying, city character varying, sex public.gender)
    LANGUAGE plpgsql
    AS $$
begin
	return query select u.id, u.username, u.first_name, u.second_name, u.birthdate, u.biography, u.city, u.sex from "User" u
	where u.id = user_id;
end;
$$;


ALTER FUNCTION public.get_user(user_id uuid) OWNER TO postgres;

--
-- Name: list_message(uuid, uuid, integer, integer); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.list_message(from_uid uuid, to_uid uuid, count_skip integer, take integer) RETURNS TABLE(id uuid, friend_id uuid, msg_text character, datesend date)
    LANGUAGE plpgsql
    AS $$
begin
	return query SELECT d.from, d.to, d.text, d.datesend
	FROM "DialogMessage" d
	where d.from = from_uid and d.to = to_uid
	ORDER BY datesend DESC
	OFFSET (@count_skip) ROWS FETCH NEXT (@Take) ROWS ONLY;
end;
$$;


ALTER FUNCTION public.list_message(from_uid uuid, to_uid uuid, count_skip integer, take integer) OWNER TO postgres;

--
-- Name: post_create(uuid, text); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.post_create(author_uid uuid, post_text text) RETURNS SETOF public."Post"
    LANGUAGE plpgsql
    AS $$
begin
	RETURN QUERY 
		INSERT INTO public."Post"( 
		"text", author_user_id)
		VALUES (post_text, author_uid)
		RETURNING *;
end;
$$;


ALTER FUNCTION public.post_create(author_uid uuid, post_text text) OWNER TO postgres;

--
-- Name: post_delete(uuid); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.post_delete(post_id uuid) RETURNS boolean
    LANGUAGE plpgsql
    AS $$
declare cnt integer;
begin
	select count(*) into cnt from "Post" p
	where p.user_id = post_id;
	
	if (cnt) = 0 then 
		raise exception 'Пост не найден';
	end if;
	
	delete from public."Post"
	WHERE "id" = post_id;
		
	return true;
end;
$$;


ALTER FUNCTION public.post_delete(post_id uuid) OWNER TO postgres;

--
-- Name: post_feed(uuid); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.post_feed(r_user_id uuid) RETURNS SETOF public."Post" LANGUAGE 'plpgsql' COST 100 VOLATILE PARALLEL UNSAFE ROWS 1000
AS $BODY$
begin
	RETURN QUERY 
	SELECT p.id, p.text, p.author_user_id, p.created_at, p.updated_at
	FROM public."Post" p
	LEFT JOIN public."Friends" f on f.user_id = r_user_id
	WHERE p.author_user_id = f.friend_id
	or p.author_user_id = r_user_id;
end;
$BODY$;

ALTER FUNCTION public.post_feed(uuid)
    OWNER TO postgres;


CREATE FUNCTION public.post_feed_except(
	r_user_id uuid,
	excluded_post_guids uuid[],
	lim integer DEFAULT 100)
    RETURNS SETOF public."Post" 
    LANGUAGE 'plpgsql'
    COST 100
    VOLATILE PARALLEL UNSAFE
    ROWS 1000

AS $BODY$
BEGIN
    RETURN QUERY 
	    SELECT p.id, p.text, p.author_user_id, p.created_at, p.updated_at
	    FROM public."Post" p
	    LEFT JOIN public."Friends" f ON f.user_id = r_user_id
	    WHERE (p.author_user_id = f.friend_id OR p.author_user_id = r_user_id)
	    AND p.id != ALL(excluded_post_guids) 
	    LIMIT lim;
END;
$BODY$;

ALTER FUNCTION public.post_feed_except(uuid, uuid[], integer)
    OWNER TO postgres;



--
-- Name: post_get(uuid); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.post_get(post_id uuid) RETURNS SETOF public."Post"
    LANGUAGE plpgsql
    AS $$

begin
	return query 
		select * from public."Post" where "id" = post_id;
end;
$$;


ALTER FUNCTION public.post_get(post_id uuid) OWNER TO postgres;

--
-- Name: post_update(uuid, text); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE OR REPLACE FUNCTION public.post_update(post_id uuid,	post_text text) RETURNS SETOF public."Post" LANGUAGE 'plpgsql'
    COST 100
    VOLATILE PARALLEL UNSAFE
    ROWS 1000

AS $BODY$
DECLARE
    cnt integer;
    r "Post"[]; 
BEGIN

    SELECT count(*) INTO cnt FROM "Post" p
    WHERE p.id = post_id;  

    IF cnt = 0 THEN 
        RAISE EXCEPTION 'Пост не найден';
    END IF;

    RETURN QUERY
		UPDATE public."Post"
	    SET "text" = post_text, "updated_at" = NOW()  
	    WHERE "id" = post_id
	    RETURNING *;
END;
$BODY$;

ALTER FUNCTION public.post_update(post_id uuid, post_text text) OWNER TO postgres;

--
-- TOC entry 222 (class 1255 OID 16529)
-- Name: get_friends(uuid); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.get_friends(current_user_id uuid) RETURNS SETOF uuid
    LANGUAGE plpgsql
    AS $$

begin
	RETURN QUERY 
		SELECT friend_id from public."Friends" where user_id = current_user_id;
end;
$$;


ALTER FUNCTION public.get_friends(current_user_id uuid) OWNER TO postgres;


CREATE OR REPLACE FUNCTION public.get_subscribers(
	current_user_id uuid)
    RETURNS SETOF uuid 
    LANGUAGE 'plpgsql'
    COST 100
    VOLATILE PARALLEL UNSAFE
    ROWS 1000

AS $BODY$

begin
	RETURN QUERY 
		SELECT user_id from public."Friends" where friend_id = current_user_id;
end;
$BODY$;

ALTER FUNCTION public.get_subscribers(uuid)
    OWNER TO postgres;

--
-- TOC entry 243 (class 1255 OID 16463)
-- Name: post_get_all(uuid); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE OR REPLACE FUNCTION public.post_get_all(user_id uuid) RETURNS SETOF public."Post" LANGUAGE 'plpgsql'
    COST 100
    VOLATILE PARALLEL UNSAFE
    ROWS 1000

AS $BODY$

begin
	return query select * from public."Post" where "author_user_id" = user_id;
end;
$BODY$;

ALTER FUNCTION public.post_get_all(uuid)
    OWNER TO postgres;
--
-- Name: register_user(character, character, date, character, character, character, character, character); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.register_user(f_name text, s_name text, dt_birth date, bio text, city text, sword text, u_name text, u_sex text) RETURNS uuid LANGUAGE 'plpgsql'
    COST 100
    VOLATILE PARALLEL UNSAFE
AS $BODY$
DECLARE 
    w int;
    res uuid;  -- Объявляем переменную res
BEGIN
    SELECT count(*) INTO w FROM "User" u
    WHERE u.username = u_name;

    IF w > 0 THEN 
        RAISE EXCEPTION 'Логин уже существует';
    END IF;

    INSERT INTO public."User"(
        first_name, second_name, birthdate, biography, city, password, username, sex)
    VALUES (f_name, s_name, dt_birth, bio, city, sword, u_name, u_sex)
    RETURNING id INTO res;

    RETURN res; 
END; 
$BODY$;


ALTER FUNCTION public.register_user(f_name text, s_name text, dt_birth date, bio text, city text, sword text, u_name text, u_sex text) OWNER TO postgres;

--
-- Name: remove_friend(uuid, uuid); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.remove_friend(current_user_id uuid, friend_user_id uuid) RETURNS boolean
    LANGUAGE plpgsql
    AS $$
declare cnt integer;
begin
	select count(*) into cnt from "Friends" f
	where f.user_id = current_user_id and f.friend_id = friend_user_id;
	
	if (cnt) > 0 then 
		DELETE FROM public."Friends" f
		where f.user_id = current_user_id and friend_id = friend_user_id;
		
		return true;
	end if;
end;
$$;


ALTER FUNCTION public.remove_friend(current_user_id uuid, friend_user_id uuid) OWNER TO postgres;

--
-- Name: send_message(uuid, uuid, character varying); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.send_message(from_uid uuid, to_uid uuid, msg character varying) RETURNS boolean
    LANGUAGE plpgsql
    AS $$
declare cnt integer;
begin
	select count(*) into cnt from "User" u
	where u.id in (from_uid, to_uid);
	
	if (cnt) <> 2 then
		raise exception 'Пользователя не существует';
	end if;
	
	INSERT INTO public."DialogMessage"(
	"from", "to", text)
	VALUES (from_uid, to_uid, msg);
	
	return true;
end;
$$;


ALTER FUNCTION public.send_message(from_uid uuid, to_uid uuid, msg character varying) OWNER TO postgres;

SET default_tablespace = '';

SET default_table_access_method = heap;


CREATE FUNCTION public.most_active(
	)
    RETURNS SETOF uuid 
    LANGUAGE 'plpgsql'
    COST 100
    VOLATILE PARALLEL UNSAFE
    ROWS 1000

AS $BODY$
begin
	RETURN QUERY SELECT ma.user_id from 
	(SELECT f.user_id, COUNT(p.id) AS post_count
FROM public."Friends" f
LEFT JOIN public."Post" p ON f.friend_id = p.author_user_id
GROUP BY f.user_id
ORDER BY post_count DESC
LIMIT 100) ma;
end;
$BODY$;

ALTER FUNCTION public.most_active() OWNER TO postgres;



--
-- Postgres metrics
--

\connect postgres

CREATE USER postgres_exporter WITH PASSWORD 'postgres_exporter';
GRANT CONNECT ON DATABASE gigglebook TO postgres_exporter;
GRANT CONNECT ON DATABASE postgres TO postgres_exporter;
GRANT USAGE ON SCHEMA public TO postgres_exporter;
GRANT SELECT ON ALL TABLES IN SCHEMA public TO postgres_exporter;
ALTER USER postgres_exporter SET SEARCH_PATH TO postgres_exporter,pg_catalog,public;
GRANT pg_monitor to postgres_exporter;
CREATE SCHEMA IF NOT EXISTS postgres_exporter;
GRANT USAGE ON SCHEMA postgres_exporter TO postgres_exporter;

-- Создание функции для получения pg_stat_activity
CREATE OR REPLACE FUNCTION public.get_pg_stat_activity() 
RETURNS SETOF pg_stat_activity AS
$$ 
SELECT * FROM pg_catalog.pg_stat_activity; 
$$ 
LANGUAGE sql
VOLATILE
SECURITY DEFINER;

-- Создание представления для pg_stat_activity
CREATE OR REPLACE VIEW postgres_exporter.pg_stat_activity AS
SELECT * FROM public.get_pg_stat_activity();

-- Предоставление прав на представление
GRANT SELECT ON postgres_exporter.pg_stat_activity TO postgres_exporter;

-- Создание функции для получения pg_stat_replication
CREATE OR REPLACE FUNCTION public.get_pg_stat_replication() 
RETURNS SETOF pg_stat_replication AS
$$ 
SELECT * FROM pg_stat_replication; 
$$ 
LANGUAGE sql
VOLATILE
SECURITY DEFINER;

-- Создание представления для pg_stat_replication
CREATE OR REPLACE VIEW postgres_exporter.pg_stat_replication AS
SELECT * FROM public.get_pg_stat_replication();

-- Предоставление прав на представление
GRANT SELECT ON postgres_exporter.pg_stat_replication TO postgres_exporter;

-- Убедитесь, что расширение pg_stat_statements установлено
SET search_path TO public;
CREATE EXTENSION IF NOT EXISTS pg_stat_statements;

-- Создание функции для получения pg_stat_statements
CREATE OR REPLACE FUNCTION public.get_pg_stat_statements() 
RETURNS SETOF pg_stat_statements AS
$$ 
SELECT * FROM pg_stat_statements; 
$$ 
LANGUAGE sql
VOLATILE
SECURITY DEFINER;

-- Создание представления для pg_stat_statements
CREATE OR REPLACE VIEW postgres_exporter.pg_stat_statements AS
SELECT * FROM public.get_pg_stat_statements();

-- Предоставление прав на представление
GRANT SELECT ON postgres_exporter.pg_stat_statements TO postgres_exporter;